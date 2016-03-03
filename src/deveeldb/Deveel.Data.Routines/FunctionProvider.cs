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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Deveel.Data.Sql;
using Deveel.Data.Sql.Fluid;
using Deveel.Data.Sql.Types;

using DryIoc;

namespace Deveel.Data.Routines {
	public abstract class FunctionProvider : IRoutineResolver, IConfigurationContext, IDisposable {
		private Container container;

		protected FunctionProvider() {
			container = new Container(Rules.Default.WithResolveIEnumerableAsLazyEnumerable());
			CallInit();
		}

		~FunctionProvider() {
			Dispose(false);
		}

		public abstract string SchemaName { get; }

		private void CallInit() {
			OnInit();
		}

		protected abstract void OnInit();

		protected virtual ObjectName NormalizeName(ObjectName name) {
			var parentName = name.Parent;
			if (parentName == null)
				return null;

			return name;
		}

		protected void Register(FunctionInfo functionInfo, Func<InvokeContext, InvokeResult> body, Func<InvokeContext, SqlType> returnType) {
			Register(new DelegateFunction(functionInfo, body, returnType));
		}

		protected void Register(Func<IFunctionConfiguration, IFunctionConfiguration> config) {
			if (config == null)
				return;

			var configuration = new FunctionConfiguration(this);
			config(configuration);

			var functionInfos = configuration.FunctionInfo;
			foreach (var functionInfo in functionInfos) {
				Register(functionInfo, configuration.ExecuteFunc, configuration.ReturnTypeFunc);
			}
		}

		protected void Register(IFunction function) {
			if (function == null)
				throw new ArgumentNullException("function");

			var functionName = function.FullName.FullName.ToUpperInvariant();
			container.RegisterInstance(function, serviceKey: functionName,reuse:Reuse.Singleton);
		}

		IRoutine IRoutineResolver.ResolveRoutine(Invoke request, IQuery query) {
			return ResolveFunction(request, query);
		}

		public IFunction ResolveFunction(Invoke invoke, IQuery query) {
			var name = NormalizeName(invoke.RoutineName);

			if (name == null ||
				!name.ParentName.Equals(SchemaName))
				return null;

			var functionName = name.FullName.ToUpperInvariant();
			var functions = container.ResolveMany<IFunction>(serviceKey:functionName).ToArrayOrSelf();
			if (functions.Length == 0)
				return null;
			if (functions.Length == 1)
				return functions[0];

			return functions.FirstOrDefault(x => x.RoutineInfo.MatchesInvoke(invoke, query));
		}

		#region DelegateFunction

		class DelegateFunction : Function {
			private readonly Func<InvokeContext, InvokeResult> functionBody;
			private readonly Func<InvokeContext, SqlType> returnType; 
 
			public DelegateFunction(FunctionInfo functionInfo, Func<InvokeContext, InvokeResult> functionBody, Func<InvokeContext, SqlType> returnType)
				: base(functionInfo) {
				this.functionBody = functionBody;
				this.returnType = returnType;
			}

			public override InvokeResult Execute(InvokeContext context) {
				return functionBody(context);
			}

			public override SqlType ReturnType(InvokeContext context) {
				if (returnType == null)
					return FunctionInfo.ReturnType;

				return returnType(context);
			}
		}

		#endregion

		#region FunctionConfiguration

		class FunctionConfiguration : IFunctionConfiguration, IRoutineConfiguration {
			private readonly FunctionProvider provider;
			private readonly Dictionary<string, RoutineParameter> parameters;
			private List<ObjectName> aliases;

			public FunctionConfiguration(FunctionProvider provider) {
				this.provider = provider;
				parameters = new Dictionary<string, RoutineParameter>();
				FunctionType = FunctionType.Static;
			}

			public FunctionType FunctionType { get; private set; }

			public Func<InvokeContext, SqlType> ReturnTypeFunc { get; private set; }

			public Func<InvokeContext, InvokeResult> ExecuteFunc { get; private set; }

			public FunctionInfo[] FunctionInfo {
				get {
					var result = new List<FunctionInfo> { new FunctionInfo(FunctionName, parameters.Values.ToArray()) };
					if (aliases != null && aliases.Count > 0)
						result.AddRange(aliases.Select(name => new FunctionInfo(name, parameters.Values.ToArray())));

					return result.ToArray();
				}
			}

			public ObjectName FunctionName { get; private set; }

			public RoutineParameter[] Parameters {
				get { return parameters.Values.ToArray(); }
			}

			public bool HasParameter(string name) {
				return parameters.ContainsKey(name);
			}

			public bool HasUnboundedParameter() {
				return parameters.Values.Any(x => x.IsUnbounded);
			}

			public IFunctionConfiguration Named(ObjectName name) {
				if (name == null)
					throw new ArgumentNullException("name");

				var parent = name.ParentName;

				if (!provider.SchemaName.Equals(parent))
					throw new ArgumentException(String.Format(
						"The parent name ({0}) is not valid in this provider schema context ({1})", parent, provider.SchemaName));

				FunctionName = name;
				return this;
			}

			public IFunctionConfiguration OfType(FunctionType functionType) {
				FunctionType = functionType;
				return this;
			}

			public IFunctionConfiguration WithAlias(ObjectName alias) {
				if (alias == null)
					throw new ArgumentNullException("alias");

				if (FunctionName == null)
					throw new ArgumentException("The function has no name configured and cannot be aliased.");

				var parent = alias.ParentName;

				if (!provider.SchemaName.Equals(parent))
					throw new ArgumentException();

				if (aliases == null)
					aliases = new List<ObjectName>();

				aliases.Add(alias);

				return this;
			}

			public IFunctionConfiguration WithParameter(Action<IFunctionParameterConfiguration> config) {
				var paramConfig = new FunctionParameterConfiguration(this);
				if (config != null) {
					config(paramConfig);

					var param = paramConfig.AsParameter();

					if (String.IsNullOrEmpty(param.Name))
						throw new InvalidOperationException("A parameter must define a name.");

					parameters.Add(param.Name, param);
				}

				return this;
			}

			public IFunctionConfiguration ReturnsType(Func<InvokeContext, SqlType> returns) {
				ReturnTypeFunc = returns;
				return this;
			}

			public IFunctionConfiguration WhenExecute(Func<InvokeContext, InvokeResult> execute) {
				ExecuteFunc = execute;
				return this;
			}

			public IConfigurationContext Context {
				get { return provider; }
			}
		}

		#endregion

		#region FunctionParemeterConfiguration

		class FunctionParameterConfiguration : IFunctionParameterConfiguration {
			private readonly FunctionConfiguration configuration;

			private string parameterName;
			private SqlType sqlType;
			private ParameterAttributes attributes;

			public FunctionParameterConfiguration(FunctionConfiguration configuration) {
				this.configuration = configuration;

				attributes = new ParameterAttributes();
				sqlType = PrimitiveTypes.Numeric();
			}

			public IFunctionParameterConfiguration Named(string name) {
				if (String.IsNullOrEmpty(name))
					throw new ArgumentNullException("name");

				if (configuration.HasParameter(name))
					throw new ArgumentException(String.Format("A parameter with name '{0}' was already configured for the function", name), "name");

				parameterName = name;

				return this;
			}

			public IFunctionParameterConfiguration OfType(SqlType type) {
				if (type == null)
					throw new ArgumentNullException("type");

				sqlType = type;

				return this;
			}

			public IFunctionParameterConfiguration Unbounded(bool flag) {
				if (configuration.HasUnboundedParameter())
					throw new ArgumentException("An unbounded parameter is already configured");

				if (flag)
					attributes |= ParameterAttributes.Unbounded;

				return this;
			}

			public RoutineParameter AsParameter() {
				return new RoutineParameter(parameterName, sqlType, attributes);
			}
		}

		#endregion

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (container != null)
					container.Dispose();
			}

			container = null;
		}
	}
}
