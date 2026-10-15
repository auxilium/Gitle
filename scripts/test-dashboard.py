"""Run dashboard aggregate regressions against an isolated SQL Server connection.
Usage: python scripts/test-dashboard.py --server "(localdb)\\GitleDashboardTests"
Only connection-local temporary tables are created; no application database is used.
"""
import argparse
import pathlib
import random
import re
import subprocess
import tempfile

root = pathlib.Path(__file__).resolve().parents[1]
source = (root / 'src/Gitle.Web/Controllers/DashboardController.cs').read_text()
method = source[source.index('private IList<DashboardProjectViewModel> LoadDashboardProjects'):]
sql = re.search(r'var sql = @"(.*?)";', method, re.S).group(1)
uninvoiced = re.search(r'const string uninvoiced = @"(.*?)";', method, re.S).group(1)
counts = re.search(r'const string counts = @"(.*?)";', method, re.S).group(1)
scale_sql = re.search(r'const string sql = @"(.*?)";', source, re.S).group(1)

def temporary_tables(query):
    for name in ['Project', 'UserProject', 'Issue', 'ChangeState', 'Booking', 'Invoice', 'InvoiceLine', 'InvoiceLineBooking']:
        query = query.replace('[' + name + ']', '#' + name)
    return query

statements = ["SET NOCOUNT ON; SET XACT_ABORT ON;", """
create table #Project (Id bigint, Name nvarchar(100), Slug nvarchar(100), Closed bit, BudgetMinutes float, IsActive bit, Type nvarchar(255));
create table #UserProject (Project_id bigint, User_id bigint);
create table #Issue (Id bigint, Project_id bigint, Administrative bit, Urgent bit, Hours float, Devvers int);
create table #ChangeState (Id bigint, Issue_id bigint, IssueState nvarchar(255), CreatedAt datetime);
create table #Booking (Id bigint, Project_id bigint, Issue_id bigint, IsActive bit, Unbillable bit, Minutes float);
create table #Invoice (Id bigint, Project_id bigint, State nvarchar(255), IsActive bit);
create table #InvoiceLine (Id bigint, Invoice_id bigint, Hours float, [Null] bit);
create table #InvoiceLineBooking (InvoiceLine_id bigint, Booking_id bigint);
create table #Actual (Id bigint, Name nvarchar(100), Slug nvarchar(100), Closed bit, Budget float, Hours float, Invoiced float,
 Estimated float, Remaining float, Completed float, OpenCount int, DoneCount int, HoldCount int, ClosedCount int, UrgentCount int, TotalCount int);
"""]

def literal(value):
    if value is None:
        return 'NULL'
    if isinstance(value, str):
        return "N'" + value.replace("'", "''") + "'"
    return str(int(value)) if isinstance(value, bool) else str(value)

def insert(table, values):
    statements.append('insert #' + table + ' values (' + ','.join(map(literal, values)) + ');')

projects = []
issues = []
bookings = []
invoices = []
lines = []
links = set()
rng = random.Random(741)
states = ['Unknown', 'Open', 'Done', 'Hold', 'Closed', 'Archived', 'Urgent']
for pid in range(1, 13):
    project = dict(id=pid, closed=pid % 2 == 0, active=pid != 11, type='Initial' if pid in (9, 10) else 'Service', allowed=pid % 3 != 0)
    projects.append(project)
    insert('Project', [pid, 'Project %02d' % pid, 'project-%d' % pid, project['closed'], 600, project['active'], project['type']])
    insert('UserProject', [pid, 1 if project['allowed'] else 2])
    # Project 12 is empty; other projects exercise all states, no history, and status changes.
    for offset in range(0 if pid == 12 else 28):
        iid = pid * 100 + offset
        state = states[offset % len(states)]
        admin = offset % 4 == 0
        urgent = offset % 5 == 0
        hours = rng.randint(0, 8)
        devvers = rng.randint(0, 3)
        issues.append(dict(id=iid, project=pid, state=state, admin=admin, urgent=urgent, estimate=hours * devvers))
        insert('Issue', [iid, pid, admin, urgent, hours, devvers])
        if state != 'Unknown':
            insert('ChangeState', [iid * 2, iid, 'Archived' if state != 'Archived' else 'Open', '2020-01-01'])
            insert('ChangeState', [iid * 2 + 1, iid, state, '2021-01-01'])
        for b in range(2):
            bid = iid * 10 + b
            booking = dict(id=bid, project=pid, issue=iid, active=offset % 6 != 0, unbillable=offset % 4 == 0, minutes=rng.randint(-1, 6) * 60)
            bookings.append(booking)
            insert('Booking', [bid, pid, iid, booking['active'], booking['unbillable'], booking['minutes']])
    # Include bookings without a ticket in project totals.
    bid = pid * 10000
    bookings.append(dict(id=bid, project=pid, issue=None, active=True, unbillable=True, minutes=30))
    insert('Booking', [bid, pid, None, True, True, 30])
    for n in range(3):
        inv = dict(id=pid * 10 + n, project=pid, state='Concept' if n == 0 else 'Definitive', active=n != 2)
        invoices.append(inv)
        insert('Invoice', [inv['id'], pid, inv['state'], inv['active']])
        for l in range(3):
            line = dict(id=inv['id'] * 10 + l, invoice=inv, hours=l + 0.5, null=l == 2)
            lines.append(line)
            insert('InvoiceLine', [line['id'], inv['id'], line['hours'], line['null']])
            for booking in [b for b in bookings if b['project'] == pid and b['issue'] and b['id'] % 3 == l]:
                insert('InvoiceLineBooking', [line['id'], booking['id']])
                if inv['state'] == 'Definitive':
                    links.add(booking['id'])

def facts(project, overview):
    tickets = [i for i in issues if i['project'] == project['id']]
    unbilled = lambda i: any(b['issue'] == i['id'] and b['active'] and not b['unbillable'] and b['minutes'] > 0 and b['id'] not in links for b in bookings)
    count = lambda predicate: sum(1 for i in tickets if predicate(i))
    counts = [
        count(lambda i: (i['state'] == 'Open' and not i['admin']) or i['state'] == 'Urgent' or i['urgent']),
        count(lambda i: i['state'] == 'Done' and not i['admin']),
        count(lambda i: i['state'] == 'Hold' and not i['admin']),
        count(lambda i: i['state'] == 'Closed' and not i['admin'] and unbilled(i)),
        count(lambda i: i['state'] == 'Urgent' and not i['admin']),
        count(lambda i: i['state'] != 'Archived' and not i['admin'] and (i['state'] != 'Closed' or unbilled(i)))] if overview else [0] * 6
    return [project['id'], 'Project %02d' % project['id'], 'project-%d' % project['id'], project['closed'], 10,
        sum(b['minutes'] for b in bookings if b['project'] == project['id'] and b['active']) / 60,
        sum(l['hours'] for l in lines if l['invoice']['project'] == project['id'] and l['invoice']['active'] and l['invoice']['state'] == 'Definitive' and not l['null']),
        sum(i['estimate'] for i in tickets if i['state'] != 'Archived'),
        sum(i['estimate'] for i in tickets if i['state'] not in ['Archived', 'Done', 'Closed']),
        sum(i['estimate'] for i in tickets if i['state'] in ['Done', 'Closed'])] + counts

parameters = {s.lower(): s for s in states}
parameters.update(issueClosed='Closed', definitive='Definitive', userId=1)
columns = ['Id', 'Name', 'Slug', 'Closed', 'Budget', 'Hours', 'Invoiced', 'Estimated', 'Remaining', 'Completed', 'OpenCount', 'DoneCount', 'HoldCount', 'ClosedCount', 'UrgentCount', 'TotalCount']
checks = 0
for admin in [False, True]:
    for project_type in ['Service', 'Initial']:
        for closed, overview in [(False, False), (True, False), (False, True)]:
            args = dict(parameters, unrestricted=admin, type=project_type, closed=closed)
            query = sql.replace('{0}', uninvoiced if overview else '0').replace('{1}', counts if overview else '0 as OpenCount, 0 as DoneCount, 0 as HoldCount, 0 as ClosedCount, 0 as UrgentCount, 0 as TotalCount')
            query = re.sub(r':(\w+)', lambda m: literal(args[m[1]]), query)
            position = query.rindex('select p.Id')
            query = query[:position] + 'insert #Actual ' + query[position:]
            statements.append(temporary_tables(query) + ';')
            expected = [facts(p, overview) for p in projects if p['active'] and p['closed'] == closed and p['type'] == project_type and (admin or p['allowed']) and any(i['project'] == p['id'] for i in issues)]
            statements.append("if (select count(*) from #Actual) <> %d throw 51000, 'Incorrect project selection', 1;" % len(expected))
            for row in expected:
                predicates = []
                for column, value in zip(columns, row):
                    predicates.append('abs(' + column + '-' + literal(value) + ') < 0.000001' if isinstance(value, (float, int)) else column + '=' + literal(value))
                statements.append("if not exists (select 1 from #Actual where " + ' and '.join(predicates) + ") throw 51000, 'Incorrect dashboard aggregate', 1;")
            statements.append('truncate table #Actual;')
            checks += 1
    args = dict(parameters, unrestricted=admin, type='Service')
    scale = re.sub(r':(\w+)', lambda m: literal(args[m[1]]), scale_sql)
    expected_scale = max([facts(p, True)[10] for p in projects if p['active'] and p['type'] == 'Service' and (admin or p['allowed'])] or [0])
    statements.append('if (' + temporary_tables(scale) + ") <> %d throw 51000, 'Incorrect overview scale', 1;" % expected_scale)
    checks += 1
statements.append("print '%d dashboard SQL scenarios passed';" % checks)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--server', required=True)
args = parser.parse_args()
with tempfile.TemporaryDirectory(prefix='gitle-dashboard-') as folder:
    file = pathlib.Path(folder) / 'regression.sql'
    file.write_text('\n'.join(statements))
    subprocess.run(['sqlcmd', '-S', args.server, '-E', '-d', 'tempdb', '-b', '-l', '10', '-i', str(file)], check=True)
