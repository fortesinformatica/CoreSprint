using System;
using System.Threading;

namespace CoreSprint.Helpers
{
    public class ExecutionHelper
    {
        public static T ExecuteAndRetryOnFail<T>(Func<T> actionToExecution, int retryTimes = 10, int waitToRetry = 500)
        {
            var retryCount = 0;
            var exception = new Exception("Error");

            while (retryCount < 10)
            {
                try
                {
                    retryCount++;
                    //Console.WriteLine("{0}ª tentativa de execução.", retryCount);
                    return actionToExecution();
                }
                catch (Exception e)
                {
                    exception = e;
                    Console.WriteLine("Ocorreu um erro: {0}\r\n{1}", e.Message, e.StackTrace);
                    if (waitToRetry > 0)
                        Thread.Sleep(waitToRetry);
                }
            }

            Console.WriteLine("Excederam o limite de {0} tentativas para executar o comando.", waitToRetry);
            throw exception;
        }

        //TODO: ver forma de remover o código duplicado
        public static void ExecuteAndRetryOnFail(Action actionToExecution, int retryTimes = 10, int waitToRetry = 500)
        {
            var retryCount = 0;
            var exception = new Exception("Error");

            while (retryCount < 10)
            {
                try
                {
                    retryCount++;
                    //Console.WriteLine("{0}ª tentativa de execução.", retryCount);
                    actionToExecution();
                    return;
                }
                catch (Exception e)
                {
                    exception = e;
                    Console.WriteLine("Ocorreu um erro: {0}\r\n{1}", e.Message, e.StackTrace);
                    if (waitToRetry > 0)
                        Thread.Sleep(waitToRetry);
                }
            }

            Console.WriteLine("Excederam o limite de {0} tentativas para executar o comando.", waitToRetry);
            throw exception;
        }
    }
}
