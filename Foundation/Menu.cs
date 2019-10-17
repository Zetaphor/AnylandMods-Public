using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnylandMods {
    public class Menu : ICollection<MenuItem> {
        private Dictionary<string, MenuItem> itemsById;

        public int Count => itemsById.Count;
        public bool IsReadOnly => false;

        public Menu()
        {
            itemsById = new Dictionary<string, MenuItem>();
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
    }
}
