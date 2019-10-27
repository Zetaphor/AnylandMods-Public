using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods {
    public class MenuDialog : CustomDialog {
        private Menu menu;
        private int currentPage = 0;
        private int pageCount = 1;
        private List<GameObject> menuItemObjects;
        public const int ItemsPerPage = 6;

        public MenuDialog()
        {
            menuItemObjects = new List<GameObject>();
        }

        public Menu Menu {
            get {
                return menu;
            }
            set {
                menu = value;
                currentPage = 0;
                pageCount = (menu.Count + ItemsPerPage - 1) / ItemsPerPage;
                if (pageCount == 0)
                    pageCount = 1;
                AddMenuItems();
            }
        }

        protected override void InitCustomDialog(object arg = null)
        {
            Menu = (Menu)arg;
        }

        public static GameObject SwitchTo(Menu menu, Hand hand = null, string tabName = "")
        {
            return CustomDialog.SwitchTo<MenuDialog>(menu, hand, tabName);
        }

        public void Start()
        {
            Init(gameObject);
            AddFundament();
            AddCloseButton();
            if (menu.backButtonAction != null)
                AddBackButton();
            AddMenuItems();
            AddDefaultPagingButtons();
        }

        private void AddMenuItems()
        {
            RemoveMenuItems();
            AddLabel("Mod Functions", 0, -420, 2.0f, textColor: TextColor.Blue, align: TextAlignment.Center, anchor: TextAnchor.MiddleCenter);
            int start = ItemsPerPage * currentPage;
            int end = Math.Min(start + ItemsPerPage, Menu.Count);
            for (int i = start; i < end; ++i) {
                menuItemObjects.Add(Menu[i].Create(this, 0, 115 * (i - start) - 305));
            }
        }

        private void RemoveMenuItems()
        {
            foreach (GameObject obj in menuItemObjects) {
                UnityEngine.Object.Destroy(obj);
            }
            menuItemObjects.Clear();
        }

        public override void OnClick(string contextName, string contextId, bool state, GameObject thisButton)
        {
            switch (contextName) {
                case "previousPage":
                    --currentPage;
                    if (currentPage < 0)
                        currentPage = pageCount - 1;
                    AddMenuItems();
                    break;
                case "nextPage":
                    ++currentPage;
                    if (currentPage >= pageCount)
                        currentPage = 0;
                    AddMenuItems();
                    break;
                case "close":
                    CloseDialog();
                    break;
                case "back":
                    menu.backButtonAction(this, hand, tabName);
                    break;
                default:
                    if (Menu.Contains(contextName)) {
                        MenuItem item = Menu[contextName];
                        if (item is MenuDataItem<bool> mdi) {
                            mdi.Value = state;
                        }
                        item.OnAction(this);
                    }
                    break;
            }
        }
    }
}
