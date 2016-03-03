﻿// 
//  Copyright 2010-2015 Deveel
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//


using System;

using Deveel.Data;
using Deveel.Data.Security;

namespace Deveel.Data.Sql.Expressions {
	/// <summary>
	/// Encapsulates the elements needed to evaluate an <see cref="SqlExpression"/>
	/// </summary>
	public sealed class EvaluateContext {
		public EvaluateContext(IRequest request, IVariableResolver variableResolver) 
			: this(request, variableResolver, null) {
		}

		public EvaluateContext(IRequest request, IVariableResolver variableResolver, IGroupResolver groupResolver) {
			GroupResolver = groupResolver;
			VariableResolver = variableResolver;
			Request = request;
		}

		/// <summary>
		/// Gets an object used to resolve variables from within the expression.
		/// </summary>
		/// <remarks>
		/// A variable can be resolved against an encapsulated context (for example a 
		/// stored procedure or a statement within the procedure), or against the
		/// global context of the system (for example a static variable of the database
		/// or a session variable).
		/// </remarks>
		public IVariableResolver VariableResolver { get; private set; }

		/// <summary>
		/// Gets the object that aggregate functions will use to resolve variable groups 
		/// </summary>
		public IGroupResolver GroupResolver { get; private set; }

		/// <summary>
		/// Gets the query in which an expression is evaluated.
		/// </summary>
		public IRequest Request { get; private set; }


		/// <summary>
		/// Gets the current user of the context.
		/// </summary>
		/// <seealso cref="User"/>
		/// <seealso cref="Security.User"/>
		public User User {
			get { return Request.User(); }
		}
	}
}