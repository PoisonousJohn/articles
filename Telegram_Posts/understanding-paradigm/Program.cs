using System;
using System.Reactive.Linq;
using System.Reactive;
using System.Threading;
using System.Reactive.Disposables;

namespace understanding_paradigm
{
    class Program
    {
        private static ILoginRepository loginRepository = new LoginRepositoryStub();
        static void Main(string[] args)
        {
            Console.WriteLine("Started the program");
            bool exit = false;
            IDisposable disposable = loginRepository.GetTokenObservable()
                .Finally(() => {
                    Console.WriteLine($"Closing token observable");
                })
                .Where(token => token != null)
                .Subscribe(token => {
                    Console.WriteLine($"Got token {token}");
                    loginRepository.fetchUserSave(token);
                },
                e => {
                    Console.WriteLine($"Got exception while getting the token: {e}");
                });

            loginRepository.GetUserSaveObservable()
                .Where(state => state != null)
                .Subscribe(state => {
                    Console.WriteLine($"User's cash: {state.cash}");
                    exit = true;
                });

            loginRepository.fetchToken("device id");
            while (!exit) {
                Thread.Sleep(1);
            }
        }
    }
}
