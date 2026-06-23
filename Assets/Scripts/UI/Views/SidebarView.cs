using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UI.Panels;

namespace UI.Views
{
    public class SidebarView : MonoBehaviour
    {
        [SerializeField] private Button logisticsButton;
        [SerializeField] private Button militaryButton;
        [SerializeField] private RectTransform sidebarPanel;
        
        private HUDViewModel hudViewModel;
        private ConstructionMenuViewModel constructionMenuViewModel;
        
        private bool isExpanded = false;

        private void Start()
        {
            // Initialize ViewModels
            hudViewModel = new HUDViewModel();
            constructionMenuViewModel = new ConstructionMenuViewModel();

            // Bind interactions
            logisticsButton.onClick.AsObservable().Subscribe(_ => ToggleCategory("Logistics")).AddTo(this);
            militaryButton.onClick.AsObservable().Subscribe(_ => ToggleCategory("Military")).AddTo(this);
            
            // Example of binding data:
            constructionMenuViewModel.AvailableBuildings.ObserveAdd()
                .Subscribe(building => { Debug.Log("Added building to sidebar: " + building.Value); })
                .AddTo(this);
        }

        private void ToggleCategory(string category)
        {
            isExpanded = !isExpanded;
            // Simple logic for expanding panel, could use DOTween in reality
            sidebarPanel.anchoredPosition = new Vector2(isExpanded ? 0 : -300, 0);
            Debug.Log($"Category {category} toggled. Expanded: {isExpanded}");
        }

        private void OnDestroy()
        {
            hudViewModel?.Dispose();
            constructionMenuViewModel?.Dispose();
        }
    }
}
