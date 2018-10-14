using System;
using System.Reactive;
using System.Reactive.Linq;
using Game.Models;

public interface ILoginRepository {
	#region commands

	void fetchToken(string deviceId);
	void fetchUserSave(string token);

	#endregion

	#region events

	IObservable<string> GetTokenObservable();
	IObservable<UserState> GetUserSaveObservable();

	#endregion
}