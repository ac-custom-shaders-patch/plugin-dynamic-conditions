using System;

namespace AcTools.ServerPlugin.DynamicConditions.Utils {
    public class Logging {
        public static void Write(object msg) {
            Console.Write(@"> ");
            Console.WriteLine(msg);
        }

        public static void Warning(object msg) {
            Console.Write(@"! ");
            Console.WriteLine(msg);
        }

        public static void Debug(object msg) {
            Console.Write(@". ");
            Console.WriteLine(msg);
        }
    }
}