namespace XboxKit
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            Options? opts = Helpers.ParseArgs(args);
            if (opts == null)
                return;

            switch (opts.Mode)
            {
                case Mode.ExtractRedump:
                    ExtractRedump.Run(opts);
                    break;
                case Mode.ExtractVideo:
                    ExtractVideo.Run(opts);
                    break;
                case Mode.ProcessXISO:
                    ProcessXISO.Run(opts);
                    break;
                case Mode.RebuildISO:
                    RebuildISO.Run(opts);
                    break;
            }
        }
    }
}
