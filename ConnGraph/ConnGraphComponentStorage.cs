using System;
using System.Collections.Generic;

namespace Connectivity
{
    public enum ConnGraphComponentStorageType
    {
        /// <summary>
        /// Do not cache components of the graph.
        /// No extra time for update but GetAllComponents() not implemented
        /// </summary>
        None = 0,

        /// <summary>
        /// Cache components using a Dictionary.
        /// O(1) extra time for update, O(N) GetAllComponents()
        /// </summary>
        Dictionary = 1,

        /// <summary>
        /// Cache components using a SortedDictionary.
        /// O(log N) extra time for update, O(C) GetAllComponents where C is the number of components
        /// </summary>
        SortedDictionary = 2,
    }

    internal abstract class ConnGraphComponentStorage
    {
        public abstract void Add(EulerTourNode root, ConnVertex vertex);
        public abstract void Add(EulerTourVertex eulerVertex, ConnVertex vertex);
        public abstract void Remove(EulerTourNode root);
        public abstract void Remove(EulerTourVertex eulerVertex);
        public abstract int GetCount();
        public abstract ICollection<ComponentInfo> GetComponents();
    }

    internal class ConnGraphComponentStorageNone : ConnGraphComponentStorage
    {
        private int _count;

        public override void Add(EulerTourNode root, ConnVertex vertex)
        {
            _count++;
        }

        public override void Add(EulerTourVertex eulerVertex, ConnVertex vertex)
        {
            _count++;
        }

        public override void Remove(EulerTourNode root)
        {
            _count--;
        }

        public override void Remove(EulerTourVertex eulerVertex)
        {
            _count--;
        }

        public override int GetCount()
        {
            return _count;
        }

        public override ICollection<ComponentInfo> GetComponents()
        {
            throw new System.NotImplementedException();
        }
    }

    internal class ConnGraphComponentStorageIDictionary : ConnGraphComponentStorage
    {
        private IDictionary<EulerTourNode, ConnVertex> _dict;

        public ConnGraphComponentStorageIDictionary(IDictionary<EulerTourNode, ConnVertex> dict)
        {
            _dict = dict;
            var st = new SortedDictionary<object, object>();
        }

        public override void Add(EulerTourNode root, ConnVertex vertex)
        {
            _dict.Add(root, vertex);
        }

        public override void Add(EulerTourVertex eulerVertex, ConnVertex vertex)
        {
            _dict.Add(eulerVertex.arbitraryVisit.Root(), vertex);
        }

        public override void Remove(EulerTourNode root)
        {
            if (!_dict.Remove(root))
            {
                throw new ArgumentException();
            }
        }

        public override void Remove(EulerTourVertex eulerVertex)
        {
            if (!_dict.Remove(eulerVertex.arbitraryVisit.Root()))
            {
                throw new ArgumentException();
            }
        }

        public override int GetCount()
        {
            return _dict.Count;
        }

        public override ICollection<ComponentInfo> GetComponents()
        {
            var infos = new List<ComponentInfo>(GetCount());
            foreach (var pair in _dict)
            {
                infos.Add(new ComponentInfo(pair.Value, pair.Key.augmentation, pair.Key.ConnGraphSize));
            }
            return infos;
        }
    }
}