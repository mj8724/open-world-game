using System;
using UniRx;
using UnityEngine;
using UI.Core;

namespace UI.Panels
{
    public class HUDViewModel : UIViewModel
    {
        public ReactiveProperty<int> PlayerHealth = new ReactiveProperty<int>(100);
        public ReactiveProperty<int> PlayerMana = new ReactiveProperty<int>(100);

        public HUDViewModel()
        {
            // Subscribe to world events
            // WorldEvents.OnPlayerHealthChanged.Where(_ => isActive).Subscribe(h => PlayerHealth.Value = h).AddTo(disposables);
        }
    }
}
