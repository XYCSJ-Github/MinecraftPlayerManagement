using g_mpm.Enums;

namespace g_mpm
{
    public class Events
    {
        public class ProgramStatusChangeed : EventArgs
        {
            public ProgramStatus ProgramStatus { get; set; }

            public ProgramStatusChangeed(ProgramStatus programStatus)
            {
                ProgramStatus = programStatus;
            }
        }
    }
}
