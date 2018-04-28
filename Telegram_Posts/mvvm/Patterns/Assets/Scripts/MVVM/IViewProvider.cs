namespace MVVM
{
    public interface IViewProvider
    {
        /// <summary>
        /// Obtain a view
        /// </summary>
        /// <param name="viewVariant">Altenative variant of the view. E.g. "HD", "ipad" or anything else</param>
        /// <returns></returns>
        T GetView<T>(string viewVariant = null) where T : UnityEngine.MonoBehaviour, IMvvmView;
        T GetView<T, M>(M viewModel, string viewVariant = null)
            where T : UnityEngine.MonoBehaviour, IMvvmView<M>
            where M: IViewModel;
    }

}