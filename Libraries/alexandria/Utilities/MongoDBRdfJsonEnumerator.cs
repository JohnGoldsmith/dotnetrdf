﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB;
using VDS.RDF;
using VDS.RDF.Parsing;
using Alexandria.Documents.Adaptors;

namespace Alexandria.Utilities
{
    class MongoDBRdfJsonEnumerator : IEnumerator<Triple>, IEnumerable<Triple>
    {
        private IEnumerator<Document> _cursor;
        private IMongoCollection _collection;
        private Document _query;
        private Queue<Triple> _buffer = null;
        private Document _nextDoc;
        private Func<Triple, bool> _selector;
        private RdfJsonParser _parser = new RdfJsonParser();

        public MongoDBRdfJsonEnumerator(IMongoCollection collection, Document query, Func<Triple,bool> selector)
        {
            this._collection = collection;
            this._query = query;
            this._selector = selector;
        }

        public Triple Current
        {
            get 
            {
                if (this._buffer == null)
                {
                    throw new InvalidOperationException("Enumerator is at the start of the collection");
                }
                else if (this._buffer.Count > 0)
                {
                    return this._buffer.Dequeue();
                }
                else
                {
                    throw new InvalidOperationException("Enumerator is at the end of the collection");
                }
            }
        }

        object System.Collections.IEnumerator.Current
        {
            get 
            {
                return this.Current;
            }
        }

        public bool MoveNext()
        {
            //If we're at the Start of the Collection need to get a enumerator of the Documents
            if (this._cursor == null)
            {
                this._cursor = this._collection.Find(this._query).Documents.GetEnumerator();

                if (this._cursor.MoveNext())
                {
                    this._nextDoc = this._cursor.Current;
                }
            }

            //If there's anything left in the buffer return the appropriate value
            if (this._buffer != null)
            {
                //If there's more than 1 item in the buffer or there are further documents to parse then return true, otherwise false
                return this._buffer.Count > 1 || this.BufferNextDocument();
            }
            else
            {
                this._buffer = new Queue<Triple>();
            }

            //Otherwise if there's a Document to be processed then we need to parse that document
            if (this._nextDoc != null)
            {
                return this.BufferNextDocument();
            }
            else
            {
                return false;
            }
        }

        private bool BufferNextDocument()
        {
            if (this._nextDoc != null)
            {
                if (this._nextDoc["graph"] == null)
                {
                    if (this._cursor.MoveNext())
                    {
                        this._nextDoc = this._cursor.Current;
                    }
                    else
                    {
                        return false;
                    }
                }

                String json = this._nextDoc["graph"].ToString();
                Graph g = new Graph();
                StringParser.Parse(g, json, this._parser);

                //Buffer Triples which match the Selector function
                foreach (Triple t in g.Triples)
                {
                    if (this._selector(t)) this._buffer.Enqueue(t);
                }

                //Get the Next Document
                if (this._cursor.MoveNext())
                {
                    this._nextDoc = this._cursor.Current;
                }
                else
                {
                    this._nextDoc = null;
                }

                //Return based on whether we buffered anything
                if (this._buffer.Count == 0)
                {
                    //If the buffer is empty but there's another document recurse to try and get triples from it
                    if (this._nextDoc != null) return this.BufferNextDocument();
                    return false;
                }
                else
                {
                    //If there's stuff in the Buffer then
                    return this._buffer.Count > 1 || this.BufferNextDocument();
                }
            }
            else
            {
                return this._buffer.Count > 1;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException("Reset() is not supported by the MongoTripleEnumerator");
        }

        public void Dispose()
        {
            if (this._cursor != null)
            {
                this._cursor.Dispose();
                this._cursor = null;
            }
            if (this._buffer != null)
            {
                this._buffer.Clear();
                this._buffer = null;
            }
        }

        public IEnumerator<Triple> GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }
    }
}
