using System;
using UniRx;
using UnityEngine;
using UI.Core;

namespace UI.Panels
{
    public class UnitPanelViewModel : UIViewModel
    {
        public ReactiveProperty<string> UnitName = new ReactiveProperty<string>("");
        public ReactiveProperty<int> UnitLevel = new ReactiveProperty<int>(1);

        public UnitPanelViewModel()
        {
            // Subscribe to selection events
            // WorldEvents.OnUnitSelected.Where(_ => isActive).Subscribe(u => { UnitName.Value = u.Name; UnitLevel.Value = u.Level; }).AddTo(disposables);
        }
    }
}
