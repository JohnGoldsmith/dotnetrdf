/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
// 
// Copyright (c) 2009-2020 dotNetRDF Project (http://dotnetrdf.org/)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using VDS.Common.References;

namespace VDS.RDF.Query.Datasets
{
    /// <summary>
    /// Abstract Base class of dataset designed around out of memory datasets where you rarely wish to load data into memory but simply wish to know which graph to look in for data.
    /// </summary>
    public abstract class BaseQuadDataset
        : ISparqlDataset
    {
        private readonly ThreadIsolatedReference<Stack<IEnumerable<IRefNode>>> _defaultGraphs;
        private readonly ThreadIsolatedReference<Stack<IEnumerable<IRefNode>>> _activeGraphs;
        private readonly bool _unionDefaultGraph = true;
        private readonly IRefNode _defaultGraphName;

        /// <summary>
        /// Creates a new Quad Dataset.
        /// </summary>
        public BaseQuadDataset()
        {
            _defaultGraphs = new ThreadIsolatedReference<Stack<IEnumerable<IRefNode>>>(InitDefaultGraphStack);
            _activeGraphs = new ThreadIsolatedReference<Stack<IEnumerable<IRefNode>>>(InitActiveGraphStack);
        }

        /// <summary>
        /// Creates a new Quad Dataset.
        /// </summary>
        /// <param name="unionDefaultGraph">Whether to make the default graph the union of all graphs.</param>
        public BaseQuadDataset(bool unionDefaultGraph)
            : this()
        {
            _unionDefaultGraph = unionDefaultGraph;
        }

        /// <summary>
        /// Creates a new Quad Dataset.
        /// </summary>
        /// <param name="defaultGraphUri">URI of the Default Graph.</param>
        public BaseQuadDataset(Uri defaultGraphUri)
            : this(false)
        {
            _defaultGraphName = new UriNode(defaultGraphUri);
        }

        /// <summary>
        /// Creates a new quad dataset.
        /// </summary>
        /// <param name="defaultGraphName">Name of the default graph.</param>
        public BaseQuadDataset(IRefNode defaultGraphName) : this(false)
        {
            _defaultGraphName = defaultGraphName;
        }

        private Stack<IEnumerable<IRefNode>> InitDefaultGraphStack()
        {
            var s = new Stack<IEnumerable<IRefNode>>();
            if (!_unionDefaultGraph)
            {
                s.Push(new [] { _defaultGraphName });
            }
            return s;
        }

        private Stack<IEnumerable<IRefNode>> InitActiveGraphStack()
        {
            return new Stack<IEnumerable<IRefNode>>();
        }

        #region Active and Default Graph management

        /// <summary>
        /// Sets the Active Graph.
        /// </summary>
        /// <param name="graphUris">Graph URIs.</param>
        [Obsolete("Replaced by SetActiveGraph(IList<IRefNode>)")]
        public void SetActiveGraph(IEnumerable<Uri> graphUris)
        {
            _activeGraphs.Value.Push(graphUris.Select(u=>u == null ? null : new UriNode(u)).ToList());
        }

        /// <summary>
        /// Sets the active graph to be the union of the graphs with the given names.
        /// </summary>
        /// <param name="graphNames"></param>
        public void SetActiveGraph(IList<IRefNode> graphNames)
        {
            _activeGraphs.Value.Push(graphNames.ToList());
        }

        /// <summary>
        /// Sets the Active Graph.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        [Obsolete("Replaced by SetActiveGraph(IRefNode)")]
        public void SetActiveGraph(Uri graphUri)
        {
            SetActiveGraph(graphUri == null ? null : new UriNode(graphUri));
        }

        /// <summary>
        /// Sets the active graph to be the graph with the given name.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        public void SetActiveGraph(IRefNode graphName)
        {
            if (graphName == null)
            {
                _activeGraphs.Value.Push(DefaultGraphNames);
            }
            else
            {
                _activeGraphs.Value.Push(new [] { graphName });
            }

        }

        /// <summary>
        /// Sets the Default Graph.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        [Obsolete("Replaced by SetDefaultGraph(IRefNode)")]
        public void SetDefaultGraph(Uri graphUri)
        {
            SetDefaultGraph(graphUri == null ? null : new UriNode(graphUri));
        }

        /// <summary>
        /// Sets the default graph to be the graph with the given name.
        /// </summary>
        /// <param name="graphName"></param>
        public void SetDefaultGraph(IRefNode graphName)
        {
            _defaultGraphs.Value.Push(new []{graphName});
        }

        /// <summary>
        /// Sets the Default Graph.
        /// </summary>
        /// <param name="graphUris">Graph URIs.</param>
        [Obsolete("Replaced by SetDefaultGraph(IList<IRefNode>)")]
        public void SetDefaultGraph(IEnumerable<Uri> graphUris)
        {
            SetDefaultGraph(graphUris.Select(x=>x== null ? null : new UriNode(x) as IRefNode).ToList());
        }

        /// <summary>
        /// Sets the default graph to be the union of the graphs with the given names.
        /// </summary>
        /// <param name="graphNames">Graph names.</param>
        public void SetDefaultGraph(IList<IRefNode> graphNames)
        {
            _defaultGraphs.Value.Push(new List<IRefNode>(graphNames));
        }

        /// <summary>
        /// Resets the Active Graph.
        /// </summary>
        public void ResetActiveGraph()
        {
            if (_activeGraphs.Value.Count > 0)
            {
                _activeGraphs.Value.Pop();
            }
            else
            {
                throw new RdfQueryException("Unable to reset the Active Graph since no previous Active Graphs exist");
            }
        }

        /// <summary>
        /// Resets the Default Graph.
        /// </summary>
        public void ResetDefaultGraph()
        {
            if (_defaultGraphs.Value.Count > 0)
            {
                _defaultGraphs.Value.Pop();
            }
            else
            {
                throw new RdfQueryException("Unable to reset the Default Graph since no previous Default Graphs exist");
            }
        }

        /// <summary>
        /// Gets the Default Graph URIs.
        /// </summary>
        [Obsolete("Replaced by GDefaultGraphNames")]
        public IEnumerable<Uri> DefaultGraphUris
        {
            get 
            {
                foreach (IRefNode name in DefaultGraphNames)
                {
                    if (name == null) yield return null;
                    if (name is IUriNode uriNode) yield return uriNode.Uri;
                }
            }
        }

        public IEnumerable<IRefNode> DefaultGraphNames
        {
            get
            {
                if (_defaultGraphs.Value.Count > 0)
                {
                    return _defaultGraphs.Value.Peek();
                }
                else if (_unionDefaultGraph)
                {
                    return GraphNames;
                }
                else
                {
                    return Enumerable.Empty<IRefNode>();
                }
            }
        }

        /// <summary>
        /// Gets the Active Graph URIs.
        /// </summary>
        [Obsolete("Replaced by ActiveGraphNames")]
        public IEnumerable<Uri> ActiveGraphUris
        {
            get 
            {
                foreach (IRefNode name in ActiveGraphNames)
                {
                    if (name == null) yield return null;
                    if (name is IUriNode uriNode) yield return uriNode.Uri;
                }
            }
        }

        /// <summary>
        /// Gets the enumeration of the names of the graphs that currently make up the active graph.
        /// </summary>
        public IEnumerable<IRefNode> ActiveGraphNames
        {
            get
            {
                if (_activeGraphs.Value.Count > 0)
                {
                    return _activeGraphs.Value.Peek();
                }
                else
                {
                    return DefaultGraphNames;
                }
            }
        }

        /// <summary>
        /// Gets whether this dataset uses a union default graph.
        /// </summary>
        public bool UsesUnionDefaultGraph
        {
            get 
            {
                return _unionDefaultGraph;
            }
        }

        /// <summary>
        /// Gets whether the given URI represents the default graph of the dataset.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <returns></returns>
        protected bool IsDefaultGraph(Uri graphUri)
        {
            return IsDefaultGraph(new UriNode(graphUri));
        }

        /// <summary>
        /// Gets whether the given name represents the default graph of the dataset.
        /// </summary>
        /// <param name="graphName"></param>
        /// <returns></returns>
        protected bool IsDefaultGraph(IRefNode graphName)
        {
            return EqualityHelper.AreRefNodesEqual(graphName, _defaultGraphName);
        }

        #endregion

        /// <summary>
        /// Adds a Graph to the dataset.
        /// </summary>
        /// <param name="g">Graph.</param>
        public virtual bool AddGraph(IGraph g)
        {
            var added = false;
            foreach (Triple t in g.Triples)
            {
                added = AddQuad(g.BaseUri, t) || added;
            }
            return added;
        }

        /// <summary>
        /// Adds a Quad to the Dataset.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <param name="t">Triple.</param>
        [Obsolete("Replaced by AddQuad(IRefNode, Triple)")]
        protected internal abstract bool AddQuad(Uri graphUri, Triple t);

        /// <summary>
        /// Adds a Quad to the Dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <param name="t">Triple.</param>
        protected internal abstract bool AddQuad(IRefNode graphName, Triple t);

        /// <summary>
        /// Removes a Graph from the Dataset.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        [Obsolete("Replaced by RemoveGraph(IRefNode)")]
        public abstract bool RemoveGraph(Uri graphUri);

        /// <summary>
        /// Removes a Graph from the Dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <exception cref="NotSupportedException">May be thrown if the Dataset is immutable i.e. Updates not supported.</exception>
        public abstract bool RemoveGraph(IRefNode graphName);

        /// <summary>
        /// Removes a Quad from the Dataset.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <param name="t">Triple.</param>
        [Obsolete("Replacecd by RemoveQuad(IRefNode, Triple)")]
        protected internal abstract bool RemoveQuad(Uri graphUri, Triple t);

        /// <summary>
        /// Removes a quad from the dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <param name="t">Triple to remove.</param>
        /// <returns></returns>
        protected internal abstract bool RemoveQuad(IRefNode graphName, Triple t);

        /// <summary>
        /// Gets whether a Graph with the given URI is the Dataset.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <returns></returns>
        [Obsolete("Replaced by HasGraph(IRefNode)")]
        public bool HasGraph(Uri graphUri)
        {
            return HasGraph(new UriNode(graphUri));
        }

        /// <summary>
        /// Gets whether a Graph with the given name is the Dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <returns></returns>
        public bool HasGraph(IRefNode graphName)
        {
            if (graphName == null)
            {
                if (DefaultGraphNames.Any())
                {
                    return true;
                }
                else
                {
                    return HasGraphInternal(null);
                }
            }
            else
            {
                return HasGraphInternal(graphName);
            }
        }

        /// <summary>
        /// Determines whether a given Graph exists in the Dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <returns></returns>
        protected abstract bool HasGraphInternal(IRefNode graphName);

        /// <summary>
        /// Gets the Graphs in the dataset.
        /// </summary>
        public virtual IEnumerable<IGraph> Graphs
        {
            get 
            {
                return from u in GraphNames select this[u];
            }
        }

        /// <summary>
        /// Gets the URIs of the graphs in the dataset.
        /// </summary>
        [Obsolete("Replaced by GraphNames")]
        public abstract IEnumerable<Uri> GraphUris
        {
            get;
        }

        /// <summary>
        /// Gets an enumeration of the names of all graphs in the dataset.
        /// </summary>
        public abstract IEnumerable<IRefNode> GraphNames { get; }

        /// <summary>
        /// Gets the Graph with the given URI from the Dataset.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// This property need only return a read-only view of the Graph, code which wishes to modify Graphs should use the <see cref="ISparqlDataset.GetModifiableGraph">GetModifiableGraph()</see> method to guarantee a Graph they can modify and will be persisted to the underlying storage.
        /// </para>
        /// </remarks>
        [Obsolete("Replaced by this[IRefNode]")]
        public virtual IGraph this[Uri graphUri]
        {
            get
            {
                return this[graphUri == null ? null : new UriNode(graphUri)];
            }
        }

        /// <summary>
        /// Gets the graph with the given name from the dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// This property need only return a read-only view of the Graph, code which wishes to modify Graphs should use the <see cref="ISparqlDataset.GetModifiableGraph(IRefNode)">GetModifiableGraph()</see> method to guarantee a Graph they can modify and will be persisted to the underlying storage.
        /// </para>
        /// </remarks>
        public virtual IGraph this[IRefNode graphName]
        {
            get
            {
                if (graphName == null)
                {
                    if (DefaultGraphNames.Any())
                    {
                        if (DefaultGraphNames.Count() == 1)
                        {
                            return new Graph(new QuadDatasetTripleCollection(this, DefaultGraphUris.First()));
                        }
                        else
                        {
                            IEnumerable<IGraph> gs = (from u in DefaultGraphNames
                                select new Graph(new QuadDatasetTripleCollection(this, u))).OfType<IGraph>();
                            return new UnionGraph(gs.First(), gs.Skip(1));
                        }
                    }
                    else
                    {
                        return GetGraphInternal(null);
                    }
                }
                else
                {
                    return GetGraphInternal(graphName);
                }
            }
        }

        /// <summary>
        /// Gets a Graph from the dataset.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <returns></returns>
        protected abstract IGraph GetGraphInternal(IRefNode graphUri);

        /// <summary>
        /// Gets a modifiable graph from the dataset.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <returns></returns>
        [Obsolete("Replaced by GetModifiableGraph(IRefNode)")]
        public virtual IGraph GetModifiableGraph(Uri graphUri)
        {
            throw new NotSupportedException("This dataset is immutable");
        }

        /// <summary>
        /// Gets the Graph with the given name from the Dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">May be thrown if the Dataset is immutable i.e. Updates not supported.</exception>
        /// <remarks>
        /// <para>
        /// Graphs returned from this method must be modifiable and the Dataset must guarantee that when it is Flushed or Disposed of that any changes to the Graph are persisted.
        /// </para>
        /// </remarks>
        public virtual IGraph GetModifiableGraph(IRefNode graphName)
        {
            throw new NotSupportedException("This dataset is immutable");
        }

        /// <summary>
        /// Gets whether the dataset has any triples.
        /// </summary>
        public virtual bool HasTriples
        {
            get 
            {
                return Triples.Any();
            }
        }

        /// <summary>
        /// Gets whether the dataset contains a triple.
        /// </summary>
        /// <param name="t">Triple.</param>
        /// <returns></returns>
        public bool ContainsTriple(Triple t)
        {
            return ActiveGraphNames.Any(u => ContainsQuad(u, t));
        }

        /// <summary>
        /// Gets whether a Triple exists in a specific Graph of the dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <param name="t">Triple.</param>
        /// <returns></returns>
        protected abstract internal bool ContainsQuad(IRefNode graphName, Triple t);

        /// <summary>
        /// Gets all triples from the dataset.
        /// </summary>
        public IEnumerable<Triple> Triples
        {
            get
            {
                return from u in ActiveGraphNames
                    from t in GetQuads(u)
                    select t;
            }
        }

        /// <summary>
        /// Gets all the Triples for a specific graph of the dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <returns></returns>
        protected abstract internal IEnumerable<Triple> GetQuads(IRefNode graphName);

        /// <summary>
        /// Gets all the Triples with a given subject.
        /// </summary>
        /// <param name="subj">Subject.</param>
        /// <returns></returns>
        public IEnumerable<Triple> GetTriplesWithSubject(INode subj)
        {
            return (from u in ActiveGraphNames
                    from t in GetQuadsWithSubject(u, subj)
                    select t);
        }

        /// <summary>
        /// Gets all the Triples with a given subject from a specific graph of the dataset.
        /// </summary>
        /// <param name="graphName">Graph URI.</param>
        /// <param name="subj">Subject.</param>
        /// <returns></returns>
        protected abstract internal IEnumerable<Triple> GetQuadsWithSubject(IRefNode graphName, INode subj);

        /// <summary>
        /// Gets all the Triples with a given predicate.
        /// </summary>
        /// <param name="pred">Predicate.</param>
        /// <returns></returns>
        public IEnumerable<Triple> GetTriplesWithPredicate(INode pred)
        {
            return (from u in ActiveGraphNames
                    from t in GetQuadsWithPredicate(u, pred)
                    select t);
        }

        /// <summary>
        /// Gets all the Triples with a given predicate from a specific graph of the dataset.
        /// </summary>
        /// <param name="graphName">Graph URI.</param>
        /// <param name="pred">Predicate.</param>
        /// <returns></returns>
        protected abstract internal IEnumerable<Triple> GetQuadsWithPredicate(IRefNode graphName, INode pred);

        /// <summary>
        /// Gets all the Triples with a given object.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        public IEnumerable<Triple> GetTriplesWithObject(INode obj)
        {
            return (from u in ActiveGraphNames
                    from t in GetQuadsWithObject(u, obj)
                    select t);
        }

        /// <summary>
        /// Gets all the Triples with a given object from a specific graph of the dataset.
        /// </summary>
        /// <param name="graphName">Graph URI.</param>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        protected abstract internal IEnumerable<Triple> GetQuadsWithObject(IRefNode graphName, INode obj);

        /// <summary>
        /// Gets all the Triples with a given subject and predicate.
        /// </summary>
        /// <param name="subj">Subject.</param>
        /// <param name="pred">Predicate.</param>
        /// <returns></returns>
        public IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subj, INode pred)
        {
            return (from u in ActiveGraphNames
                    from t in GetQuadsWithSubjectPredicate(u, subj, pred)
                    select t);
        }

        /// <summary>
        /// Gets all the Triples with a given subject and predicate from a specific graph of the dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <param name="subj">Subject.</param>
        /// <param name="pred">Predicate.</param>
        /// <returns></returns>
        protected abstract internal IEnumerable<Triple> GetQuadsWithSubjectPredicate(IRefNode graphName, INode subj, INode pred);

        /// <summary>
        /// Gets all the Triples with a given subject and object.
        /// </summary>
        /// <param name="subj">Subject.</param>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        public IEnumerable<Triple> GetTriplesWithSubjectObject(INode subj, INode obj)
        {
            return (from u in ActiveGraphNames
                    from t in GetQuadsWithSubjectObject(u, subj, obj)
                    select t);
        }

        /// <summary>
        /// Gets all the Triples with a given subject and object from a specific graph of the dataset.
        /// </summary>
        /// <param name="graphName">Graph name.</param>
        /// <param name="subj">Subject.</param>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        protected abstract internal IEnumerable<Triple> GetQuadsWithSubjectObject(IRefNode graphName, INode subj, INode obj);

        /// <summary>
        /// Gets all the Triples with a given predicate and object.
        /// </summary>
        /// <param name="pred">Predicate.</param>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        public IEnumerable<Triple> GetTriplesWithPredicateObject(INode pred, INode obj)
        {
            return (from u in ActiveGraphNames
                    from t in GetQuadsWithPredicateObject(u, pred, obj)
                    select t);
        }

        /// <summary>
        /// Gets all the Triples with a given predicate and object from a specific graph of the dataset.
        /// </summary>
        /// <param name="graphName">Graph URI.</param>
        /// <param name="pred">Predicate.</param>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        protected abstract internal IEnumerable<Triple> GetQuadsWithPredicateObject(IRefNode graphName, INode pred, INode obj);

        /// <summary>
        /// Flushes any changes to the dataset.
        /// </summary>
        public virtual void Flush()
        {
            // Nothing to do
        }

        /// <summary>
        /// Discards any changes to the dataset.
        /// </summary>
        public virtual void Discard()
        {
            // Nothing to do
        }
    }
}
