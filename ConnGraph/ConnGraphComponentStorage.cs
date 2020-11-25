using System;
using System.Collections.Generic;

namespace Connectivity
{
    /// <summary>
    /// GetAllComponents() needs extra augmentation to the graph.
    /// </summary>
    public enum ConnGraphComponentStorageType
    {
        /// <summary>
        /// Do not cache components of the graph.
        /// No extra time for update but <see cref="ConnGraph.GetAllComponents"/> is not implemented
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Cache components using a Dictionary.
        /// O(1) extra time for updates with high probability,
        /// O(C) <see cref="ConnGraph.GetAllComponents"/> where C is the number of components.
        /// O(C) extra space.
        /// </summary>
        Dictionary = 1,
    }

    internal interface IConnGraphComponentStorage
    {
        void Add(EulerTourNode root, ConnVertex vertex);
        void Add(EulerTourVertex eulerVertex, ConnVertex vertex);
        void Remove(EulerTourNode root);
        void Remove(EulerTourVertex eulerVertex);
        int GetCount();
        void Optimize();
        IList<ComponentInfo> GetComponents();
    }

    internal class ConnGraphComponentStorageDisabled : IConnGraphComponentStorage
    {
        private int _count;
        public void Add(EulerTourNode root, ConnVertex vertex) => _count++;
        public void Add(EulerTourVertex eulerVertex, ConnVertex vertex) => _count++;
        public void Remove(EulerTourNode root) => _count--;
        public void Remove(EulerTourVertex eulerVertex) => _count--;
        public int GetCount() => _count;
        public void Optimize() { }
        public IList<ComponentInfo> GetComponents() => throw new NotImplementedException();
    }

    internal abstract class ConnGraphComponentStorageIDictionary<T>
        : IConnGraphComponentStorage where T : IDictionary<EulerTourNode, ConnVertex>
    {
        protected T _dict;

        protected ConnGraphComponentStorageIDictionary(T dict)
        {
            _dict = dict;
        }

        public virtual void Add(EulerTourNode root, ConnVertex vertex)
        {
            _dict.Add(root, vertex);
        }

        public void Add(EulerTourVertex eulerVertex, ConnVertex vertex)
        {
            Add(eulerVertex.arbitraryVisit.Root(), vertex);
        }

        public virtual void Remove(EulerTourNode root)
        {
            if (!_dict.Remove(root))
            {
                throw new ArgumentException();
            }
        }

        public void Remove(EulerTourVertex eulerVertex)
        {
            Remove(eulerVertex.arbitraryVisit.Root());
        }

        public int GetCount()
        {
            return _dict.Count;
        }

        public virtual void Optimize()
        {
        }

        public IList<ComponentInfo> GetComponents()
        {
            var infos = new List<ComponentInfo>(GetCount());
            foreach (var pair in _dict)
            {
                infos.Add(new ComponentInfo(pair.Value, pair.Key.augmentation, pair.Key.ConnGraphSize));
            }
            return infos;
        }
    }

    internal class ConnGraphComponentStorageDictionary : ConnGraphComponentStorageIDictionary<Dictionary<EulerTourNode, ConnVertex>>
    {
        protected const int initialCapacity = 32;
        protected int _capacity = initialCapacity;

        public ConnGraphComponentStorageDictionary() : base(new Dictionary<EulerTourNode, ConnVertex>(initialCapacity))
        {
        }

        public override void Add(EulerTourNode root, ConnVertex vertex)
        {
            base.Add(root, vertex);
            _capacity = Math.Max(_capacity, _dict.Count);
        }

        /// <summary>
        /// The cost of calling foreach on Dictionary is proportional to the capacity of the dictionary.
        /// Therefore we want to shrink _dict when its capacity is unnecessarily large.
        /// We create a new dictionary every time since Dictionary can only grow in size.
        /// </summary>
        /// <seealso cref="Dictionary{TKey,TValue}.Enumerator"/>
        public override void Optimize()
        {
            int count = _dict.Count;
            if (count * 4 < _capacity && _capacity > initialCapacity * 2)
            {
                // resize the dictionary
                var oldDict = _dict;
                _capacity = Math.Max(count * 2, initialCapacity);
                _dict = new Dictionary<EulerTourNode, ConnVertex>(_capacity);
                foreach (var pair in oldDict)
                {
                    _dict.Add(pair.Key, pair.Value);
                }
            }
        }
    }
}