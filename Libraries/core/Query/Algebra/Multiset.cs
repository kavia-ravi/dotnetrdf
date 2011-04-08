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
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Query.Algebra
{
    /// <summary>
    /// Represents a Multiset of possible solutions
    /// </summary>
    public class Multiset : BaseMultiset
    {
        /// <summary>
        /// Variables contained in the Multiset
        /// </summary>
        protected List<String> _variables = new List<string>();
        /// <summary>
        /// Dictionary of Sets in the Multiset
        /// </summary>
        protected Dictionary<int, Set> _sets = new Dictionary<int,Set>();
        /// <summary>
        /// Counter used to assign Set IDs
        /// </summary>
        protected int _counter = 0;
        /// <summary>
        /// List of IDs that is used to return the Sets in order if the Multiset has been sorted
        /// </summary>
        protected List<int> _orderedIDs = null;

        /// <summary>
        /// Creates a new Empty Multiset
        /// </summary>
        public Multiset() { }

        /// <summary>
        /// Creates a new Empty Mutliset that has the list of given Variables
        /// </summary>
        /// <param name="variables"></param>
        public Multiset(IEnumerable<String> variables)
        {
            foreach (String var in variables)
            {
                this._variables.Add(var);
            }
        }

        /// <summary>
        /// Creates a new Multiset from a SPARQL Result Set
        /// </summary>
        /// <param name="results">Result Set</param>
        internal Multiset(SparqlResultSet results)
        {
            foreach (String var in results.Variables)
            {
                this.AddVariable(var);
            }
            foreach (SparqlResult r in results.Results)
            {
                this.Add(new Set(r));
            }
        }

        /// <summary>
        /// Creates a new Multiset by flattening a Group Multiset
        /// </summary>
        /// <param name="multiset">Group Multiset</param>
        internal Multiset(GroupMultiset multiset)
        {
            foreach (String var in multiset.Variables)
            {
                this.AddVariable(var);
            }
            foreach (Set s in multiset.Sets)
            {
                this.Add(s);
            }
        }

        /// <summary>
        /// Joins this Multiset to another Multiset
        /// </summary>
        /// <param name="other">Other Multiset</param>
        /// <returns></returns>
        public override BaseMultiset Join(BaseMultiset other)
        {
            //If the Other is the Identity Multiset the result is this Multiset
            if (other is IdentityMultiset) return this;
            //If the Other is the Null Multiset the result is the Null Multiset
            if (other is NullMultiset) return other;
            //If the Other is Empty then the result is the Null Multiset
            if (other.IsEmpty) return new NullMultiset();

            //Find the First Variable from this Multiset which is in both Multisets
            //If there is no Variable from this Multiset in the other Multiset then this
            //should be a Join operation instead of a LeftJoin
            List<String> joinVars = this._variables.Where(v => other.Variables.Contains(v)).ToList();
            if (joinVars.Count == 0) return this.Product(other);

            //Start building the Joined Set
            Multiset joinedSet = new Multiset();
            foreach (Set x in this.Sets)
            {
                //New way of selecting Sets to join with (means the Distinct() in the next loop is not needed)
                //For sets to be compatible for every joinable variable they must meet one of the 3 criteria:
                //1 - Both the LHS and RHS have a null as the value
                //2 - The RHS is null (this allows for situations where the RHS may not return a value for some variables as they may be in an OPTIONAL
                //3 - Both the LHS and RHS have the same non-null value
                IEnumerable<Set> ys = other.Sets.Where(s => joinVars.All(v => (x[v] == null && s[v] == null) || s[v] == null || (x[v] != null && x[v].Equals(s[v]))));

                foreach (Set y in ys)
                {
                    joinedSet.Add(new Set(x, y));
                }
            }
            return joinedSet;
        }

        /// <summary>
        /// Does a Left Join of this Multiset to another Multiset where the Join is predicated on the given Expression
        /// </summary>
        /// <param name="other">Other Multiset</param>
        /// <param name="expr">Expression</param>
        /// <returns></returns>
        public override BaseMultiset LeftJoin(BaseMultiset other, ISparqlExpression expr)
        {
            //If the Other is the Identity/Null Multiset the result is this Multiset
            if (other is IdentityMultiset) return this;
            if (other is NullMultiset) return this;

            //Find the First Variable from this Multiset which is in both Multisets
            List<String> joinableVars = this._variables.Where(v => other.Variables.Contains(v)).ToList();
            String joinVar = joinableVars.FirstOrDefault();
            if (joinVar != null) joinableVars.RemoveAt(0);
            bool disjoint = this.IsDisjointWith(other);

            //Start building the Joined Set
            Multiset joinedSet = new Multiset();
            LeviathanLeftJoinBinder binder = new LeviathanLeftJoinBinder(joinedSet);
            SparqlEvaluationContext subcontext = new SparqlEvaluationContext(binder);
            foreach (Set x in this.Sets)
            {
                if (joinVar != null)
                {
                    //Retrieve the Node that is the value for the Join Variable
                    //If the value is null for this Set we can't Left Join it
                    INode joinNode = x[joinVar];
                    if (joinNode != null)
                    {
                        //Get all the Sets from the Other Multiset which have the given Node as their value
                        //for the Join Variable
                        IEnumerable<Set> ys = other.Sets.Where(s => joinNode.Equals(s[joinVar]) && joinableVars.All(v => x[v] == null || (x[v] != null && x[v].Equals(s[v]))));
                        if (ys.Any())
                        {
                            foreach (Set y in ys)
                            {
                                Set z = new Set(x, y);
                                try
                                {
                                    joinedSet.Add(z);
                                    if (!expr.EffectiveBooleanValue(subcontext, z.ID))
                                    {
                                        //If the Expression evaluates to false we just preserve the LHS set
                                        joinedSet.Remove(z.ID);
                                        joinedSet.Add(x);
                                        break;
                                    }
                                }
                                catch (RdfQueryException)
                                {
                                    //Only add LHS to Joined Set if the Expression errors
                                    joinedSet.Remove(z.ID);
                                    joinedSet.Add(x);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            //If there's no possible joins then the value of the expression is irrelevant
                            //since we just keep the LHS since the RHS is irrelevant to us
                            joinedSet.Add(x);
                        }
                    }
                }
                else if (disjoint)
                {
                    foreach (Set y in other.Sets)
                    {
                        Set z = new Set(x, y);
                        try
                        {
                            joinedSet.Add(z);
                            if (!expr.EffectiveBooleanValue(subcontext, z.ID))
                            {
                                //If the Expression evaluates to false we just preserve the LHS set
                                joinedSet.Remove(z.ID);
                                joinedSet.Add(x);
                                break;
                            }
                        }
                        catch (RdfQueryException)
                        {
                            joinedSet.Remove(z.ID);
                            joinedSet.Add(x);
                            break;
                        }
                    }
                }
            }
            return joinedSet;
        }

        /// <summary>
        /// Does an Exists Join of this Multiset to another Multiset where the Join is predicated on the existence/non-existence of a joinable solution on the RHS
        /// </summary>
        /// <param name="other">Other Multiset</param>
        /// <param name="mustExist">Whether a solution must exist in the Other Multiset for the join to be made</param>
        /// <returns></returns>
        public override BaseMultiset ExistsJoin(BaseMultiset other, bool mustExist)
        {
            //For EXISTS and NOT EXISTS if the other is the Identity then it has no effect
            if (other is IdentityMultiset) return this;
            if (mustExist)
            {
                //If an EXISTS then Null/Empty Other results in Null
                if (other is NullMultiset) return other;
                if (other.IsEmpty) return new NullMultiset();
            }
            else
            {
                //If a NOT EXISTS then Null/Empty results in this
                if (other is NullMultiset) return this;
                if (other.IsEmpty) return this;
            }

            //Find the Variables that are to be used for Joining
            List<String> joinVars = this._variables.Where(v => other.Variables.Contains(v)).ToList();
            if (joinVars.Count == 0)
            {
                //All Disjoint Solutions are compatible
                if (mustExist)
                {
                    //If an EXISTS and disjoint then result is this
                    return this;
                }
                else
                {
                    //If a NOT EXISTS and disjoint then result is null
                    return new NullMultiset();
                }
            }

            //Start building the Joined Set
            Multiset joinedSet = new Multiset();
            foreach (Set x in this.Sets)
            {
                //New ExistsJoin() logic based on the improved Join() logic
                bool exists = other.Sets.Any(s => joinVars.All(v => x[v] == null || s[v] == null || x[v].Equals(s[v])));

                if (exists)
                {
                    //If there are compatible sets and this is an EXIST then preserve the solution
                    if (mustExist) joinedSet.Add(x);
                }
                else
                {
                    //If there are no compatible sets and this is a NOT EXISTS then preserve the solution
                    if (!mustExist) joinedSet.Add(x);
                }
            }
            return joinedSet;
        }

        /// <summary>
        /// Does a Minus Join of this Multiset to another Multiset where any joinable results are subtracted from this Multiset to give the resulting Multiset
        /// </summary>
        /// <param name="other">Other Multiset</param>
        /// <returns></returns>
        public override BaseMultiset MinusJoin(BaseMultiset other)
        {
            //If the other Multiset is the Identity/Null Multiset then minus-ing it doesn't alter this set
            if (other is IdentityMultiset) return this;
            if (other is NullMultiset) return this;
            //If the other Multiset is disjoint then minus-ing it also doesn't alter this set
            if (this.IsDisjointWith(other)) return this;

            //Find the Variables that are to be used for Joining
            List<String> joinVars = this._variables.Where(v => other.Variables.Contains(v)).ToList();
            if (joinVars.Count == 0) return this.Product(other);

            //Start building the Joined Set
            Multiset joinedSet = new Multiset();
            foreach (Set x in this.Sets)
            {
                //New Minus logic based on the improved Join() logic
                bool minus = other.Sets.Any(s => joinVars.All(v => x[v] == null || s[v] == null || x[v].Equals(s[v])));

                //If no compatible sets then this set is preserved
                if (!minus)
                {
                    joinedSet.Add(x);
                }
            }
            return joinedSet;
        }

        /// <summary>
        /// Does a Product of this Multiset and another Multiset
        /// </summary>
        /// <param name="other">Other Multiset</param>
        /// <returns></returns>
        public override BaseMultiset Product(BaseMultiset other)
        {
            if (other is IdentityMultiset) return this;
            if (other is NullMultiset) return other;
            if (other.IsEmpty) return new NullMultiset();

            Multiset productSet = new Multiset();
            foreach (Set x in this.Sets)
            {
                foreach (Set y in other.Sets)
                {
                    productSet.Add(new Set(x, y));
                }
            }
            return productSet;
        }

        /// <summary>
        /// Does a Union of this Multiset and another Multiset
        /// </summary>
        /// <param name="other">Other Multiset</param>
        /// <returns></returns>
        public override BaseMultiset Union(BaseMultiset other)
        {
            if (other is IdentityMultiset) return this;
            if (other is NullMultiset) return this;
            if (other.IsEmpty) return this;

            foreach (Set s in other.Sets)
            {
                this.Add(s);
            }
            return this;
        }

        /// <summary>
        /// Determines whether a given Value is present for a given Variable in any Set in this Multiset
        /// </summary>
        /// <param name="var">Variable</param>
        /// <param name="n">Value</param>
        /// <returns></returns>
        public override bool ContainsValue(String var, INode n)
        {
            if (this._variables.Contains(var))
            {
                return this._sets.Values.Any(s => n.Equals(s[var]));
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns whether a given Variable is present in any Set in this Multiset
        /// </summary>
        /// <param name="var">Variable</param>
        /// <returns></returns>
        public override bool ContainsVariable(string var)
        {
            return this._variables.Contains(var);
        }

        /// <summary>
        /// Determines whether this Multiset is disjoint with another Multiset
        /// </summary>
        /// <param name="other">Other Multiset</param>
        /// <returns></returns>
        public override bool IsDisjointWith(BaseMultiset other)
        {
            if (other is IdentityMultiset || other is NullMultiset) return false;
 
            return this._variables.All(v => !other.ContainsVariable(v));
        }

        /// <summary>
        /// Adds a Set to the Multiset
        /// </summary>
        /// <param name="s">Set</param>
        public override void Add(Set s)
        {
            this._counter++;
            this._sets.Add(this._counter, s);
            s.ID = this._counter;
            foreach (String var in s.Variables)
            {
                if (!this._variables.Contains(var)) this._variables.Add(var);
            }
        }

        /// <summary>
        /// Adds a Variable to the list of Variables present in this Multiset
        /// </summary>
        /// <param name="variable">Variable</param>
        public override void AddVariable(string variable)
        {
            if (!this._variables.Contains(variable)) this._variables.Add(variable);
        }

        /// <summary>
        /// Removes a Set from the Multiset
        /// </summary>
        /// <param name="id">Set ID</param>
        public override void Remove(int id)
        {
            if (this._sets.ContainsKey(id))
            {
                this._sets.Remove(id);
                if (this._orderedIDs != null)
                {
                    this._orderedIDs.Remove(id);
                }
            }
        }

        /// <summary>
        /// Sorts a Set based on the given Comparer
        /// </summary>
        /// <param name="comparer">Comparer on Sets</param>
        public override void Sort(IComparer<Set> comparer)
        {
            if (comparer != null)
            {
                this._orderedIDs = (from s in this._sets.Values.OrderBy(x => x, comparer)
                                    select s.ID).ToList();
            }
            else
            {
                this._orderedIDs = null;
            }
        }

        /// <summary>
        /// Trims the Multiset to remove Temporary Variables
        /// </summary>
        public override void Trim()
        {
            foreach (String var in this._variables)
            {
                if (var.StartsWith("_:"))
                {
                    foreach (Set s in this._sets.Values)
                    {
                        s.Remove(var);
                    }
                }
            }
            this._variables.RemoveAll(v => v.StartsWith("_:"));
        }

        /// <summary>
        /// Trims the Multiset to remove the given Variable
        /// </summary>
        /// <param name="variable">Variable</param>
        public override void Trim(String variable)
        {
            if (this._variables.Remove(variable))
            {
                foreach (Set s in this._sets.Values)
                {
                    s.Remove(variable);
                }
            }
        }

        /// <summary>
        /// Gets whether the Multiset is empty
        /// </summary>
        public override bool IsEmpty
        {
            get 
            {
                return (this._sets.Count == 0);
            }
        }

        /// <summary>
        /// Gets the number of Sets in the Multiset
        /// </summary>
        public override int Count
        {
            get
            {
                return this._sets.Count;
            }
        }

        /// <summary>
        /// Gets the Variables in the Multiset
        /// </summary>
        public override IEnumerable<string> Variables
        {
            get 
            {
                return (from var in this._variables
                        select var);
            }
        }

        /// <summary>
        /// Gets the Sets in the Multiset
        /// </summary>
        public override IEnumerable<Set> Sets
        {
            get 
            {
                if (this._orderedIDs == null)
                {
                    return (from s in this._sets.Values
                            select s);
                }
                else
                {
                    return (from id in this._orderedIDs
                            select this._sets[id]);
                }
            }
        }

        /// <summary>
        /// Gets the IDs of Sets in the Multiset
        /// </summary>
        public override IEnumerable<int> SetIDs
        {
            get 
            {
                if (this._orderedIDs == null)
                {
                    return (from id in this._sets.Keys
                            select id);
                }
                else
                {
                    return this._orderedIDs;
                }
            }
        }

        /// <summary>
        /// Gets a Set from the Multiset
        /// </summary>
        /// <param name="id">Set ID</param>
        /// <returns></returns>
        public override Set this[int id]
        {
            get 
            {
                if (this._sets.ContainsKey(id))
                {
                    return this._sets[id];
                }
                else
                {
                    throw new RdfQueryException("A Set with ID " + id + " does not exist in this Multiset");
                }
            }
        }
    }
}