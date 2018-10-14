using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Game.Models;

public class LoginRepositoryStub : ILoginRepository
{

	// нужно учитывать, что при подписке может прийти null
	private BehaviorSubject<string> _tokenSubject = new BehaviorSubject<string>(null);
	// нужно учитывать, что при подписке может прийти null
	private BehaviorSubject<UserState> _userStateSubject = new BehaviorSubject<UserState>(null);

	private Action getTokenHandler;

	private void ReturnToken() {
		_tokenSubject.OnNext("user_stub_token");
	}

	public IObservable<string> GetTokenObservable()
	{
		return _tokenSubject;
	}

	public IObservable<UserState> GetUserSaveObservable()
	{
		return _userStateSubject;
	}

	public void fetchToken(string deviceId)
	{
		// вся логика работы с транспортом должна уйти на этот слой
		Observable.Timer(TimeSpan.FromSeconds(1))
			.Subscribe(__ => {
				ReturnToken();
			});
	}

	public void fetchUserSave(string token)
	{
		// вся логика работы с транспортом должна уйти на этот слой
		Observable.Timer(TimeSpan.FromSeconds(1))
			.Subscribe(__ => {
				_userStateSubject.OnNext(new UserState());
			});
	}
}