using System;
using UniRx;
using UnityEngine;

namespace UI.Core
{
    public abstract class UIViewModel : IDisposable
    {
        public virtual bool isActive { get; set; }
        
        protected CompositeDisposable disposables = new CompositeDisposable();

        public virtual void Dispose()
        {
            disposables.Dispose();
        }
    }
}
