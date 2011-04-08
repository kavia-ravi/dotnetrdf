﻿/*

Copyright Robert Vesse 2009-10
rvesse@vdesign-studios.com

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace VDS.RDF
{
    public abstract class BaseLiteralNode : BaseNode, ILiteralNode, IEquatable<BaseLiteralNode>, IComparable<BaseLiteralNode>
    {
        private String _value;
        private String _language;
        private Uri _datatype;

        /// <summary>
        /// Constants used to add salt to the hashes of different Literal Nodes
        /// </summary>
        private const String LangSpecLiteralHashCodeSalt = "languageSpecified",
                             DataTypedLiteralHashCodeSalt = "typed",
                             PlainLiteralHashCodeSalt = "plain";

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        protected internal BaseLiteralNode(IGraph g, String literal)
            : this(g, literal, Options.LiteralValueNormalization) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="normalize">Whether to Normalize the Literal Value</param>
        protected internal BaseLiteralNode(IGraph g, String literal, bool normalize)
            : base(g, NodeType.Literal)
        {
            if (normalize)
            {
#if !NO_NORM
            this._value = literal.Normalize();
#else
            this._value = literal;
#endif
            } 
            else 
            {
                this._value = literal;
            }
            this._language = String.Empty;
            this._datatype = null;

            //Compute Hash Code
            this._hashcode = (this._nodetype + this.ToString() + PlainLiteralHashCodeSalt).GetHashCode();
        }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="langspec">String value for the Language Specifier for the Literal</param>
        protected internal BaseLiteralNode(IGraph g, String literal, String langspec)
            : this(g, literal, langspec, Options.LiteralValueNormalization) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="langspec">String value for the Language Specifier for the Literal</param>
        /// <param name="normalize">Whether to Normalize the Literal Value</param>
        protected internal BaseLiteralNode(IGraph g, String literal, String langspec, bool normalize)
            : base(g, NodeType.Literal)
        {
            if (normalize)
            {
#if !NO_NORM
                this._value = literal.Normalize();
#else
            this._value = literal;
#endif
            }
            else
            {
                this._value = literal;
            }
            this._language = langspec;
            this._datatype = null;

            //Compute Hash Code
            if (langspec.Equals(String.Empty))
            {
                //Empty Language Specifier equivalent to a Plain Literal
                this._hashcode = (this._nodetype + this.ToString() + PlainLiteralHashCodeSalt).GetHashCode();
            }
            else
            {
                this._hashcode = (this._nodetype + this.ToString() + LangSpecLiteralHashCodeSalt).GetHashCode();
            }
        }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="datatype">Uri for the Literals Data Type</param>
        protected internal BaseLiteralNode(IGraph g, String literal, Uri datatype)
            : this(g, literal, datatype, Options.LiteralValueNormalization) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="datatype">Uri for the Literals Data Type</param>
        /// <param name="normalize">Whether to Normalize the Literal Value</param>
        protected internal BaseLiteralNode(IGraph g, String literal, Uri datatype, bool normalize)
            : base(g, NodeType.Literal)
        {
            if (normalize)
            {
#if !NO_NORM
                this._value = literal.Normalize();
#else
            this._value = literal;
#endif
            }
            else
            {
                this._value = literal;
            }
            this._language = String.Empty;
            this._datatype = datatype;

            //Compute Hash Code
            this._hashcode = (this._nodetype + this.ToString() + DataTypedLiteralHashCodeSalt).GetHashCode();
        }

        /// <summary>
        /// Gives the String Value of the Literal
        /// </summary>
        public String Value
        {
            get
            {
                return _value;
            }
        }

        /// <summary>
        /// Gives the Language Specifier for the Literal (if it exists) or the Empty String
        /// </summary>
        public String Language
        {
            get
            {
                return _language;
            }
        }

        /// <summary>
        /// Gives the Data Type Uri for the Literal (if it exists) or a null
        /// </summary>
        public Uri DataType
        {
            get
            {
                return _datatype;
            }
        }



        /// <summary>
        /// Implementation of the Equals method for Literal Nodes
        /// </summary>
        /// <param name="obj">Object to compare the Node with</param>
        /// <returns></returns>
        /// <remarks>
        /// The default behaviour is for Literal Nodes to be considered equal IFF
        /// <ol>
        /// <li>Their Language Specifiers are identical (or neither has a Language Specifier)</li>
        /// <li>Their Data Types are identical (or neither has a Data Type)</li>
        /// <li>Their String values are identical</li>
        /// </ol>
        /// This behaviour can be overridden to use value equality by setting the <see cref="Options.LiteralEqualityMode">LiteralEqualityMode</see> option to be <see cref="LiteralEqualityMode.Loose">Loose</see> if this is more suited to your application.
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            if (ReferenceEquals(this, obj)) return true;

            if (obj is INode)
            {
                return this.Equals((INode)obj);
            }
            else
            {
                //Can only be equal to other Nodes
                return false;
            }
        }

        /// <summary>
        /// Implementation of the Equals method for Literal Nodes
        /// </summary>
        /// <param name="other">Object to compare the Node with</param>
        /// <returns></returns>
        /// <remarks>
        /// The default behaviour is for Literal Nodes to be considered equal IFF
        /// <ol>
        /// <li>Their Language Specifiers are identical (or neither has a Language Specifier)</li>
        /// <li>Their Data Types are identical (or neither has a Data Type)</li>
        /// <li>Their String values are identical</li>
        /// </ol>
        /// This behaviour can be overridden to use value equality by setting the <see cref="Options.LiteralEqualityMode">LiteralEqualityMode</see> option to be <see cref="LiteralEqualityMode.Loose">Loose</see> if this is more suited to your application.
        /// </remarks>
        public override bool Equals(INode other)
        {
            if ((Object)other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            if (other.NodeType == NodeType.Literal)
            {
                return this.Equals((ILiteralNode)other);
            }
            else
            {
                //Can only be equal to a LiteralNode
                return false;
            }
        }

        public override bool Equals(IBlankNode other)
        {
            if (ReferenceEquals(this, other)) return true;
            return false;
        }

        public override bool Equals(IGraphLiteralNode other)
        {
            if (ReferenceEquals(this, other)) return true;
            return false;
        }

        public override bool Equals(ILiteralNode other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            return EqualityHelper.AreLiteralsEqual(this, other);
        }

        public override bool Equals(IUriNode other)
        {
            if (ReferenceEquals(this, other)) return true;
            return false;
        }

        public override bool Equals(IVariableNode other)
        {
            if (ReferenceEquals(this, other)) return true;
            return false;
        }

        public bool Equals(BaseLiteralNode other)
        {
            return this.Equals((ILiteralNode)other);
        }

        /// <summary>
        /// Gets a String representation of a Literal Node
        /// </summary>
        /// <returns></returns>
        /// <remarks>Gives a value without quotes (as some syntaxes use) with the Data Type/Language Specifier appended using Notation 3 syntax</remarks>
        public override string ToString()
        {
            StringBuilder stringOut = new StringBuilder();
            stringOut.Append(this._value);
            if (!this._language.Equals(String.Empty))
            {
                stringOut.Append("@");
                stringOut.Append(this._language);
            }
            else if (!(this._datatype == null))
            {
                stringOut.Append("^^");
                stringOut.Append(this._datatype.ToString());
            }

            return stringOut.ToString();
        }

        /// <summary>
        /// Implementation of CompareTo for Literal Nodes
        /// </summary>
        /// <param name="other">Node to Compare To</param>
        /// <returns></returns>
        /// <remarks>
        /// Literal Nodes are greater than Blank Nodes, Uri Nodes and Nulls, they are less than Graph Literal Nodes.
        /// <br /><br />
        /// Two Literal Nodes are initially compared based upon Data Type, untyped literals are less than typed literals.  Two untyped literals are compared purely on lexical value, Language Specifier has no effect on the ordering.  This means Literal Nodes are only partially ordered, for example "hello"@en and "hello"@en-us are considered to be the same for ordering purposes though they are different for equality purposes.  Datatyped Literals can only be properly ordered if they are one of a small subset of types (Integers, Booleans, Date Times, Strings and URIs).  If the datatypes for two Literals are non-matching they are ordered on Datatype Uri, this ensures that each range of Literal Nodes is sorted to some degree.  Again this also means that Literals are partially ordered since unknown datatypes will only be sorted based on lexical value and not on actual value.
        /// </remarks>
        public override int CompareTo(INode other)
        {
            if (ReferenceEquals(this, other)) return 0;

            if (other == null)
            {
                //Everything is greater than a null
                //Return a 1 to indicate this
                return 1;
            }
            else if (other.NodeType == NodeType.Blank || other.NodeType == NodeType.Variable || other.NodeType == NodeType.Uri)
            {
                //Literal Nodes are greater than Blank, Variable and Uri Nodes
                //Return a 1 to indicate this
                return 1;
            }
            else if (other.NodeType == NodeType.Literal)
            {
                return this.CompareTo((ILiteralNode)other);
            }
            else
            {
                //Anything else is considered greater than a Literal Node
                //Return -1 to indicate this
                return -1;
            }
        }

        public override int CompareTo(IBlankNode other)
        {
            if (ReferenceEquals(this, other)) return 0;
            //We are always greater than nulls/Blank Nodes
            return 1;
        }

        public override int CompareTo(ILiteralNode other)
        {
            if (ReferenceEquals(this, other)) return 0;

            return ComparisonHelper.CompareLiterals(this, other);
        }

        public override int CompareTo(IGraphLiteralNode other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other == null)
            {
                //We are always greater than nulls
                return 1;
            }
            else
            {
                //Graph Literals are always greater than us
                return -1;
            }
        }

        public override int CompareTo(IUriNode other)
        {
            if (ReferenceEquals(this, other)) return 0;
            //We are always greater than nulls/URI Nodes
            return 1;
        }

        public override int CompareTo(IVariableNode other)
        {
            if (ReferenceEquals(this, other)) return 0;
            //We are always greater than nulls/Variable Nodes
            return 1;
        }

        public int CompareTo(BaseLiteralNode other)
        {
            return this.CompareTo((ILiteralNode)other);
        }
    }

    /// <summary>
    /// Class for representing Literal Nodes
    /// </summary>
    public class LiteralNode : BaseLiteralNode, IEquatable<LiteralNode>, IComparable<LiteralNode>
    {
        private String _value;
        private String _language;
        private Uri _datatype;

        /// <summary>
        /// Constants used to add salt to the hashes of different Literal Nodes
        /// </summary>
        private const String LangSpecLiteralHashCodeSalt = "languageSpecified",
                             DataTypedLiteralHashCodeSalt = "typed",
                             PlainLiteralHashCodeSalt = "plain";

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        protected internal LiteralNode(IGraph g, String literal)
            : this(g, literal, Options.LiteralValueNormalization) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="normalize">Whether to Normalize the Literal Value</param>
        protected internal LiteralNode(IGraph g, String literal, bool normalize)
            : base(g, literal, normalize) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="langspec">String value for the Language Specifier for the Literal</param>
        protected internal LiteralNode(IGraph g, String literal, String langspec)
            : this(g, literal, langspec, Options.LiteralValueNormalization) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="langspec">String value for the Language Specifier for the Literal</param>
        /// <param name="normalize">Whether to Normalize the Literal Value</param>
        protected internal LiteralNode(IGraph g, String literal, String langspec, bool normalize)
            : base(g, literal, langspec, normalize) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="datatype">Uri for the Literals Data Type</param>
        protected internal LiteralNode(IGraph g, String literal, Uri datatype)
            : this(g, literal, datatype, Options.LiteralValueNormalization) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="datatype">Uri for the Literals Data Type</param>
        /// <param name="normalize">Whether to Normalize the Literal Value</param>
        protected internal LiteralNode(IGraph g, String literal, Uri datatype, bool normalize)
            : base(g, literal, datatype, normalize) { }

        /// <summary>
        /// Implementation of Compare To for Literal Nodes
        /// </summary>
        /// <param name="other">Literal Node to Compare To</param>
        /// <returns></returns>
        /// <remarks>
        /// Simply invokes the more general implementation of this method
        /// </remarks>
        public int CompareTo(LiteralNode other)
        {
            return this.CompareTo((ILiteralNode)other);
        }

        public bool Equals(LiteralNode other)
        {
            return base.Equals((ILiteralNode)other);
        }
    }

    /// <summary>
    /// Class for representing Literal Nodes where the Literal values are not normalized
    /// </summary>
    class NonNormalizedLiteralNode : LiteralNode, IComparable<NonNormalizedLiteralNode>
    {
        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        protected internal NonNormalizedLiteralNode(IGraph g, String literal)
            : base(g, literal, false) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="langspec">Lanaguage Specifier for the Literal</param>
        protected internal NonNormalizedLiteralNode(IGraph g, String literal, String langspec)
            : base(g, literal, langspec, false) { }

        /// <summary>
        /// Internal Only Constructor for Literal Nodes
        /// </summary>
        /// <param name="g">Graph this Node is in</param>
        /// <param name="literal">String value of the Literal</param>
        /// <param name="datatype">Uri for the Literals Data Type</param>
        protected internal NonNormalizedLiteralNode(IGraph g, String literal, Uri datatype)
            : base(g, literal, datatype, false) { }

        /// <summary>
        /// Implementation of Compare To for Literal Nodes
        /// </summary>
        /// <param name="other">Literal Node to Compare To</param>
        /// <returns></returns>
        /// <remarks>
        /// Simply invokes the more general implementation of this method
        /// </remarks>
        public int CompareTo(NonNormalizedLiteralNode other)
        {
            return this.CompareTo((INode)other);
        }
    }
}