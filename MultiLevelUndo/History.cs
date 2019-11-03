using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.MultiLevelUndo {

    struct ThingPartStateHistoryEntry {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public Color color;

        public ThingPartStateHistoryEntry(Vector3 position, Vector3 rotation, Vector3 scale, Color color)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.color = color;
        }

        public ThingPartStateHistoryEntry(ThingPartState state)
        {
            position = state.position;
            rotation = state.rotation;
            scale = state.scale;
            color = state.color;
        }

        public override string ToString()
        {
            return String.Format("P{0} R{1} S{2} C{3}", position, rotation, scale, color);
        }
    }

    public class UndoHistory<T> {
        private Stack<T> past, future;

        public UndoHistory()
        {
            past = new Stack<T>();
            future = new Stack<T>();
        }

        public int UndoCount {
            get {
                return past.Count;
            }
        }

        public int RedoCount {
            get {
                return future.Count;
            }
        }

        public T Undo(T currentState)
        {
            T state = past.Pop();
            future.Push(currentState);
            return state;
        }

        public T Redo(T currentState)
        {
            T state = future.Pop();
            past.Push(currentState);
            return state;
        }

        public void PushState(T state)
        {
            past.Push(state);
            future.Clear();
        }
    }

    public class HistoryBook<TSubject, THistEntry> {
        public delegate string IdentityFunc(TSubject obj);

        private Dictionary<string, UndoHistory<THistEntry>> dict;

        public IdentityFunc Identity { get; private set; }

        public HistoryBook(IdentityFunc idfunc)
        {
            Identity = idfunc;
            dict = new Dictionary<string, UndoHistory<THistEntry>>();
        }

        public UndoHistory<THistEntry> GetHistory(TSubject subject)
        {
            string id = Identity(subject);
            try {
                return dict[id];
            } catch (KeyNotFoundException) {
                var hist = new UndoHistory<THistEntry>();
                dict[id] = hist;
                return hist;
            }
        }

        public int UndoCount(TSubject subject) => GetHistory(subject).UndoCount;
        public int RedoCount(TSubject subject) => GetHistory(subject).RedoCount;
        public THistEntry Undo(TSubject subject, THistEntry currentState) => GetHistory(subject).Undo(currentState);
        public THistEntry Redo(TSubject subject, THistEntry currentState) => GetHistory(subject).Redo(currentState);
        public void PushState(TSubject subject, THistEntry state) => GetHistory(subject).PushState(state);
    }

}
