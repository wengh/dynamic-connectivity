using System;
using System.Collections.Generic;

namespace Connectivity
{
    public enum ConnGraphComponentStorageType
    {
        /// <summary>
        /// Do not cache components of the graph.
        /// No extra time for update but GetAllComponents() is not implemented
        /// </summary>
        None = 0,

        /// <summary>
        /// Cache components using a Dictionary.
        /// O(1) extra time for update with high probability, O(C) GetAllComponents() where C is the number of components.
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
        void PossiblyShrink();
        ICollection<ComponentInfo> GetComponents();
    }

    internal class ConnGraphComponentStorageNone : IConnGraphComponentStorage
    {
        private int _count;

        public void Add(EulerTourNode root, ConnVertex vertex)
        {
            _count++;
        }

        public void Add(EulerTourVertex eulerVertex, ConnVertex vertex)
        {
            _count++;
        }

        public void Remove(EulerTourNode root)
        {
            _count--;
        }

        public void Remove(EulerTourVertex eulerVertex)
        {
            _count--;
        }

        public int GetCount()
        {
            return _count;
        }

        public void PossiblyShrink()
        {
        }

        public ICollection<ComponentInfo> GetComponents()
        {
            throw new System.NotImplementedException();
        }
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

        public virtual void PossiblyShrink()
        {
        }

        public ICollection<ComponentInfo> GetComponents()
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

        public override void PossiblyShrink()
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