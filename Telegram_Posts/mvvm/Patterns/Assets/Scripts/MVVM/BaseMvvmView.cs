using UnityEngine;

namespace MVVM
{
    public abstract class BaseMvvmView<M> : MonoBehaviour, IMvvmView<M>
        where M : IViewModel
    {
        // ovverride this method to initalize your bindings
        public abstract void Init(M viewModel);

        public virtual void Init(IViewModel viewModel)
        {
            Init((M)viewModel);
        }
    }
}