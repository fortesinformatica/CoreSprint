using System;
using System.Threading;

namespace CoreSprint.Helpers
{
    public class ExecutionHelper
    {
        public static void ExecuteAndRetryOnFail(Action actionToExecution, int retryTimes = 10, int waitToRetry = 500)
        {
            var success = false;
            var retryCount = 0;
            var exception = new Exception("Error");

            while (!success && retryCount < 10)
            {
                try
                {
                    retryCount++;
                    //Console.WriteLine("{0}ª tentativa de execução.", retryCount);
                    actionToExecution();
                    success = true;
                }
                catch (Exception e)
                {
                    success = false;
                    exception = e;
                    Console.WriteLine("Ocorreu um erro: {0}\r\n{1}", e.Message, e.StackTrace);
                    if (waitToRetry > 0)
                        Thread.Sleep(waitToRetry);
                }
            }

            if (!success)
            {
                Console.WriteLine("Excederam o limite de {0} tentativas para executar o comando.", waitToRetry);
                throw exception;
            }
        }
    }
}
