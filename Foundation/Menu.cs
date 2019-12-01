using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace AnylandMods {
    public class Menu : ICollection<MenuItem> {
        public delegate void ButtonAction(MenuDialog dialog, Hand hand = null, string tabName = "");
        #region Back button handler classes

        private struct BackToVanillaDialog {
            private DialogType dialogType;
            
            public BackToVanillaDialog(DialogType dialogType)
            {
                this.dialogType = dialogType;
            }

            public void HandleBackButton(MenuDialog dialog, Hand hand = null, string tabName = "")
            {
                Managers.dialogManager.SwitchToNewDialog(dialogType, hand, tabName);
            }
        }

        private struct BackToCustomDialog {
            private MethodInfo method;
            private object arg;

            public BackToCustomDialog(Type dialogClass, object dialogArg = null)
            {
                method = typeof(CustomDialog).GetMethod(nameof(CustomDialog.SwitchTo), BindingFlags.Public | BindingFlags.Static)
                    .MakeGenericMethod(new Type[] { dialogClass });
                arg = dialogArg;
            }

            public void HandleBackButton(MenuDialog dialog, Hand hand = null, string tabName = "")
            {
                method.Invoke(null, new object[] { arg, hand, tabName });
            }
        }

        private struct BackToMenuDialog {
            private Menu menu;

            public BackToMenuDialog(Menu menu)
            {
                this.menu = menu;
            }

            public void HandleBackButton(MenuDialog dialog, Hand hand = null, string tabName = "")
            {
                MenuDialog.SwitchTo(menu, hand, tabName);
            }
        }

        #endregion

        private Dictionary<string, MenuItem> itemsById;
        internal ButtonAction backButtonAction = null;

        public event Action<MenuDialog> DialogDestroy;
        public event Action<MenuDialog> DialogClose;

        public int Count => itemsById.Count;
        public bool IsReadOnly => false;

        public string Title { get; set; }
        public bool TwoColumns { get; set; } = false;

        public Menu(string title = "Mod Functions")
        {
            itemsById = new Dictionary<string, MenuItem>();
            Title = title;
        }

        public void SetBackButton(ButtonAction onClickAction)
        {
            backButtonAction = onClickAction;
        }

        public void RemoveBackButton()
        {
            backButtonAction = null;
        }

        public void SetBackButton(DialogType dialogType)
        {
            backButtonAction = new BackToVanillaDialog(dialogType).HandleBackButton;
        }

        public void SetBackButton(Type dialogClass, object dialogArg = null)
        {
            backButtonAction = new BackToCustomDialog(dialogClass, dialogArg).HandleBackButton;
        }

        public void SetBackButton(Menu menu)
        {
            backButtonAction = new BackToMenuDialog(menu).HandleBackButton;
        }

        public void Add(MenuItem item)
        {
            if (itemsById.ContainsKey(item.Id)) {
                throw new InvalidOperationException("Menu item with ID \"" + item.Id + "\" already exists!");
            } else {
                itemsById.Add(item.Id, item);
            }
        }

        public void Clear()
        {
            itemsById.Clear();
        }

        public bool Contains(MenuItem item)
        {
            return itemsById.ContainsKey(item.Id);
        }

        public bool Contains(string id)
        {
            return itemsById.ContainsKey(id);
        }

        public void CopyTo(MenuItem[] array, int arrayIndex)
        {
            itemsById.Values.CopyTo(array, arrayIndex);
        }

        public IEnumerator<MenuItem> GetEnumerator()
        {
            return itemsById.Values.GetEnumerator();
        }

        public bool Remove(MenuItem item)
        {
            return itemsById.Remove(item.Id);
        }

        public bool Remove(string id)
        {
            return itemsById.Remove(id);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public MenuItem this[string id] {
            get {
                return itemsById[id];
            }
        }

        public MenuItem this[int index] {
            get {
                return itemsById.Values.ElementAt(index);
            }
        }

        internal void TriggerDialogDestroyEvent(MenuDialog dialog)
        {
            DialogDestroy?.Invoke(dialog);
        }

        internal void TriggerDialogCloseEvent(MenuDialog dialog)
        {
            DialogClose?.Invoke(dialog);
        }
    }
}
