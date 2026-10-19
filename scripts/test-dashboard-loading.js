// Regression tests for the actual dashboard loader, without a browser or application database.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const view = fs.readFileSync(path.join(__dirname, '../src/Gitle.Web/Views/Dashboard/index.brail'), 'utf8');
const script = view.match(/<script>([\s\S]*?)<\/script>/)[1];
// Parse all fragment scripts as well, catching syntax mistakes in the deferred DataTable setup.
for (const file of ['_issues.brail']) {
  const html = fs.readFileSync(path.join(__dirname, '../src/Gitle.Web/Views/Dashboard', file), 'utf8');
  for (const match of html.matchAll(/<script>([\s\S]*?)<\/script>/g)) new vm.Script(match[1]);
}
function setup(hash = '', admin = true) {
  class Node {
    constructor() { this.length = 1; this.store = {}; this.handlers = {}; this.nodes = []; this.visible = true; }
    data(key, value) { if (arguments.length === 1) return this.store[key]; this.store[key] = value; return this; }
    removeData(key) { delete this.store[key]; return this; }
    on(event, selector, fn) { this.handlers[event] = fn || selector; return this; }
    click(fn) { this.handlers.click = fn; return this; }
    children() { return group(this.nodes); }
    hide() { this.visible = false; return this; }
    show() { this.visible = true; return this; }
    remove() { if (this.parent) this.parent.nodes = this.parent.nodes.filter(n => n !== this); return this; }
    appendTo(parent) { parent.nodes.push(this); this.parent = parent; return this; }
    html(value) { this.markup = value; return this; }
    text(value) { this.markup = value; return this; }
    is() { return !!this.checked; }
    closest() { return this.panel; }
    attr() { return this.href; }
    addBack() { return this; }
    first() { return this; }
    foundation() { return this; }
    toggleClass() { return this; }
    find(selector) {
      if (selector === '.dashboard-closed') return this.checkbox;
      if (selector === '.dashboard-results') return this.results;
      if (selector === '.dashboard-status') return group(this.nodes.filter(n => n.status));
      if (selector === 'a') return this.link;
      throw Error('Unexpected selector ' + selector);
    }
  }
  function group(nodes) {
    const result = new Node(); result.nodes = nodes; result.length = nodes.length;
    result.each = fn => { nodes.forEach(n => fn.call(n)); return result; };
    result.hide = () => { nodes.forEach(n => n.hide()); return result; };
    result.remove = () => { nodes.forEach(n => n.remove()); return result; };
    return result;
  }
  const panels = {}, links = [], tabs = new Node(), requests = [], document = new Node();
  for (const [id, section] of [['progress-initial', 'initial'], ['progress-service', 'progress'], ['service', 'overview'], ['issues', 'issues']]) {
    if (!admin && section === 'initial') continue;
    const panel = panels['#' + id] = new Node();
    panel.data('section', section); panel.results = new Node(); panel.checkbox = new Node(); panel.checkbox.panel = panel;
    panel.link = new Node(); panel.link.href = '#' + id; links.push(panel.link);
  }
  const linkGroup = group(links), checkboxGroup = new Node();
  function $(arg) {
    if (typeof arg === 'function') { arg(); return; }
    if (typeof arg !== 'string') return arg;
    if (arg === '.tabs') return tabs;
    if (arg === '.tabs a') return linkGroup;
    if (arg === '.dashboard-closed') return checkboxGroup;
    if (arg === '.tabs-content > .active') return panels[admin ? '#progress-initial' : '#progress-service'];
    if (panels[arg]) return panels[arg];
    if (arg.startsWith('<')) { const node = new Node(); node.status = arg.includes('dashboard-status'); return node; }
    if (arg.includes('tickets-table')) return new Node();
    throw Error('Unexpected selector ' + arg);
  }
  $.get = (url, data) => {
    assert.equal(url, '/dashboard/section');
    const request = { data, done(fn) { this.success = fn; return this; }, fail(fn) { this.failure = fn; return this; },
      always(fn) { this.complete = fn; return this; },
      resolve(html = '<table>data</table>') { this.success(html); this.complete(); },
      reject() { this.failure({}, 'error'); this.complete(); },
      abort() { this.aborted = true; this.failure({}, 'abort'); this.complete(); } };
    requests.push(request); return request;
  };
  vm.runInNewContext(script, { $, document, window: { location: { hash } } });
  return { panels, requests, click(id) { linkGroup.handlers.click.call(panels[id].link); },
    closed(id, checked) { panels[id].checkbox.checked = checked; checkboxGroup.handlers.change.call(panels[id].checkbox); } };
}
const app = setup();
assert.deepEqual(app.requests.map(r => r.data.section), ['initial'], 'Only the default tab loads');
app.click('#progress-initial');
assert.equal(app.requests.length, 1, 'Repeated events reuse a pending request');
app.requests[0].resolve();
app.click('#progress-service');
assert.equal(app.requests.length, 2);
app.requests[1].resolve();
app.click('#progress-initial');
assert.equal(app.requests.length, 2, 'Revisiting a tab reuses its content');
app.closed('#progress-initial', true);
assert.equal(app.requests[2].data.closed, true);
app.closed('#progress-initial', false);
assert.equal(app.requests[2].aborted, true, 'Returning to cached open projects cancels the closed-project request');
assert.equal(app.panels['#progress-initial'].results.nodes.filter(n => n.visible).length, 1);
app.closed('#progress-initial', true);
app.requests[3].resolve();
app.closed('#progress-initial', false);
app.closed('#progress-initial', true);
assert.equal(app.requests.length, 4, 'Both open and closed results are cached');
assert.equal(app.panels['#progress-initial'].results.nodes.filter(n => n.visible).length, 1);
app.click('#service'); app.requests[4].reject();
const status = app.panels['#service'].results.nodes.find(n => n.status);
status.nodes[0].handlers.click();
assert.equal(app.requests.length, 6, 'Failed requests can be retried');
app.requests[5].resolve();
app.click('#issues'); app.requests[6].resolve('<table id="project-table"></table>');
app.click('#service'); app.click('#issues');
assert.equal(app.requests.length, 7, 'The ticket-list fragment initializes only once');
for (const hash of ['#issues', '#fndtn-issues']) {
  const deepLink = setup(hash);
  assert.equal(deepLink.requests.length, 1);
  assert.equal(deepLink.requests[0].data.section, 'issues', 'Deep links load only the requested tab');
}
assert.equal(setup('', false).requests[0].data.section, 'progress', 'Non-admins start on their service projects');
console.log('Dashboard loading regressions passed (lazy tabs, closed projects, cache, abort, retry, deep links).');
