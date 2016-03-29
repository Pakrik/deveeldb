// 
//  Copyright 2010-2016 Deveel
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

namespace Deveel.Data.Sql.Expressions {
	/// <summary>
	/// An interface used to prepare a <see cref="SqlExpression"/> object.
	/// </summary>
	/// <remarks>
	/// This interface is used to mutate an expression of an <see cref="SqlExpression"/>
	/// from one form to another.
	/// </remarks>
	public interface IExpressionPreparer {
		/// <summary>
		/// Verifies whether the instance of the interface can prepare
		/// the given expression.
		/// </summary>
		/// <param name="expression">The expression object to verify.</param>
		/// <returns>
		/// Returns <b>true</b> if this preparer will prepare the given object in 
		/// an expression.
		/// </returns>
		bool CanPrepare(SqlExpression expression);

		/// <summary>
		/// Returns the new translated object to be mutated from the given expression.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		SqlExpression Prepare(SqlExpression expression);
	}
}