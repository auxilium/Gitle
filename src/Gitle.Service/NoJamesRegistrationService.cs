namespace Gitle.Service
{
    using System;
    using Model.Interfaces.Service;

    public class NoJamesRegistrationService : IJamesRegistrationService
    {
        #region Implementation of IJamesRegistrationService

        public int GetTotalMinutesForEmployee(int jamesEmployeeId, int year, int week)
        {
            return 0;
        }

        public int GetTotalMinutesForEmployee(int jamesEmployeeId, DateTime day)
        {
            return 0;
        }

        #endregion
    }
}