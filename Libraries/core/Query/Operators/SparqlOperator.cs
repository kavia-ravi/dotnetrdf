﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Nodes;
using VDS.RDF.Query.Operators.Numeric;

namespace VDS.RDF.Query.Operators
{
    /// <summary>
    /// Registry of SPARQL Operands
    /// </summary>
    public static class SparqlOperators
    {
        private static Dictionary<SparqlOperatorType, List<ISparqlOperator>> _operators = new Dictionary<SparqlOperatorType, List<ISparqlOperator>>();
        private static bool _init = false;

        /// <summary>
        /// Initializes the Operators registry
        /// </summary>
        private static void Init()
        {
            if (_init) return;
            lock (_operators)
            {
                if (_init) return;

                //Set up empty registry for each operator type
                foreach (SparqlOperatorType type in Enum.GetValues(typeof(SparqlOperatorType)).OfType<SparqlOperatorType>())
                {
                    _operators.Add(type, new List<ISparqlOperator>());
                }

                //Register default operators
                _operators[SparqlOperatorType.Add].Add(new AdditionOperator());
                _operators[SparqlOperatorType.Subtract].Add(new SubtractionOperator());

                _init = true;
            }
        }

        /// <summary>
        /// Registers a new operator
        /// </summary>
        /// <param name="type">Operator Type</param>
        /// <param name="operand">Operator</param>
        public static void AddOperator(SparqlOperatorType type, ISparqlOperator op)
        {
            if (!_init) Init();
            lock (_operators)
            {
                _operators[type].Add(op);
            }
        }

        /// <summary>
        /// Removes the registration of an operator
        /// </summary>
        /// <param name="type">Operator Type</param>
        /// <param name="operand">Operator</param>
        public static void RemoveOperand(SparqlOperatorType type, ISparqlOperator op)
        {
            if (!_init) Init();
            lock (_operators)
            {
                _operators[type].Remove(op);
            }
        }

        /// <summary>
        /// Tries to return the operator which applies for the given inputs
        /// </summary>
        /// <param name="type">Operator Type</param>
        /// <param name="op">Operator</param>
        /// <param name="ns">Inputs</param>
        /// <returns></returns>
        public static bool TryGetOperand(SparqlOperatorType type, out ISparqlOperator op, params IValuedNode[] ns)
        {
            op = null;
            List<ISparqlOperator> ops;
            if (_operators.TryGetValue(type, out ops))
            {
                foreach (ISparqlOperator possOp in ops)
                {
                    if (possOp.IsApplicable(ns))
                    {
                        op = possOp;
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }
    }
}