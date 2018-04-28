using UnityEngine;
using System.Reflection;
using System;

namespace MVVM
{
    public class DefaultViewProvider : IViewProvider
    {
        public DefaultViewProvider(Func<Type, IViewModel> viewModelProvider)
        {
            _viewModelProvider = viewModelProvider;
        }

        public T GetView<T>(string viewVariant = null) where T : MonoBehaviour, IMvvmView
        {
            Type tType = typeof(T);
            if (_defaultAssembly == null)
            {
                _defaultAssembly = tType.Assembly;
            }
            string modelTypeName = tType.Name + "Model";
            Type modelType = _defaultAssembly.GetType(modelTypeName);
            var viewModel = _viewModelProvider(modelType);

            return (T)GetView(tType, viewModel, viewVariant);
        }

        public T GetView<T, M>(M viewModel, string viewVariant = null)
            where T : MonoBehaviour, IMvvmView<M>
            where M : IViewModel
        {
            return (T)GetView(typeof(T), viewModel, viewVariant);
        }

        public IMvvmView GetView(Type viewType, IViewModel viewModel, string viewVariant = null)
        {
            string viewPath = viewType.Name;
            if (!string.IsNullOrEmpty(viewVariant))
            {
                viewPath += viewVariant;
            }
            var view = Resources.Load(viewPath, viewType) as IMvvmView;
            view.Init(viewModel);
            return view;
        }

        private Assembly _defaultAssembly;
        private Func<Type, IViewModel> _viewModelProvider;
    }
}
