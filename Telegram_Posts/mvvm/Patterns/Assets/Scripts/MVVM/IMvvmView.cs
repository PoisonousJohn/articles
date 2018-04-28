namespace MVVM
{
	public interface IMvvmView
	{
		void Init(IViewModel viewModel);
	}

	public interface IMvvmView<T> : IMvvmView
		where T: IViewModel
	{
		void Init(T viewModel);
	}
}