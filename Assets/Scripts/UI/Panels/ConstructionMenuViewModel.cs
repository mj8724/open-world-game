using System;
using UniRx;
using UnityEngine;
using UI.Core;

namespace UI.Panels
{
    public class ConstructionMenuViewModel : UIViewModel
    {
        public ReactiveCollection<string> AvailableBuildings = new ReactiveCollection<string>();

        public ConstructionMenuViewModel()
        {
            // Mock data
            AvailableBuildings.Add("Wood Cabin");
            AvailableBuildings.Add("Stone Wall");
        }
    }
}
