// <copyright file="Command.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a command line interface command.</summary>

using Dataescher.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dataescher.CommandLineInterface {
	/// <content>A command line interface.</content>
	public partial class CLI {
		/// <summary>A command line interface command.</summary>
		public partial class Command {
			/// <summary>Gets or sets the handler attribute.</summary>
			internal HandlerAttribute HandlerInfo { get; set; }

			/// <summary>Gets or sets the handler parameter info.</summary>
			internal ParameterInfo[] Parameters { get; set; }

			/// <summary>Gets or sets the handler.</summary>
			internal MethodInfo Handler { get; set; }

			/// <summary>Gets the parent CLI.</summary>
			public CLI Parent { get; private set; }

			/// <summary>Determine if this command handles a short switch.</summary>
			/// <param name="sw">The short switch.</param>
			/// <returns>True if handled, false otherwise.</returns>
			public Boolean Handles(Char sw) {
				return HandlerInfo.ShortSwitches.Contains(sw);
			}

			/// <summary>Determine if this command handles a long switch.</summary>
			/// <param name="sw">The long switch.</param>
			/// <returns>True if handled, false otherwise.</returns>
			public Boolean Handles(String sw) {
				return HandlerInfo.LongSwitches.Contains(sw);
			}

			/// <summary>Initializes a new instance of the Dataescher.CLI.Command class.</summary>
			/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
			/// <param name="parent">The parent CLI.</param>
			/// <param name="handlerMethod">The method which handles the command.</param>
			internal Command(CLI parent, MethodInfo handlerMethod) {
				Parent = parent;
				// Double-check to ensure that the method contains the Handler attribute
				Handler = handlerMethod;
				if (handlerMethod.GetCustomAttribute<HandlerAttribute>(false) is not HandlerAttribute handlerInfo) {
					throw new Exception("Cannot add method which does not contain Handler attribute.");
				}
				HandlerInfo = handlerInfo;
				// Check to ensure that all parameters of the method contain the Parameter attribute,
				// and that parameter types are supported
				Boolean sawArrayParam = false;
				Parameters = Handler.GetParameters();
				Int32 paramIdx = 0;
				foreach (ParameterInfo paramInfo in Parameters) {
					Type paramType = paramInfo.ParameterType;
					if (paramInfo.ParameterType.IsArray) {
						if (sawArrayParam) {
							throw new Exception("Only the last parameter of a command handler can be an array type.");
						}
						sawArrayParam = true;
					}
					if (paramInfo.GetCustomAttribute<ParameterAttribute>(false) is not ParameterAttribute paramAttr) {
						throw new Exception(
							$"Method \"{handlerMethod.Name}\" missing Parameter attribute on parameter #{paramIdx + 1}. " +
							$"If a method uses the Handler attribute, all parameters must use the Parameter attribute."
						);
					}
					paramIdx++;
				}
			}

			/// <summary>Gets the help text.</summary>
			/// <param name="sb">The string builder.</param>
			internal void GetHelpText(StringBuilder sb) {
				sb.AppendLine(HandlerInfo.Description);

				Boolean firstSwitch = true;
				// Build the switch screen
				foreach (Char thisShortSwitch in HandlerInfo.ShortSwitches.OrderBy(x => x)) {
					if (!firstSwitch) {
						sb.Append(", ");
					}
					firstSwitch = false;
					sb.Append($"-{thisShortSwitch}");
				}
				foreach (String thisLongSwitch in HandlerInfo.LongSwitches.OrderBy(x => x)) {
					if (!firstSwitch) {
						sb.Append(", ");
					}
					firstSwitch = false;
					sb.Append($"--{thisLongSwitch}");
				}
				foreach (ParameterInfo paramInfo in Parameters) {
					sb.Append($" <{paramInfo.Name}>");
				}
				sb.AppendLine();

				if (Parameters.Length > 0) {
					sb.AppendLine("Parameters:");
					foreach (ParameterInfo paramInfo in Parameters) {
						ParameterAttribute paramAttr = paramInfo.GetCustomAttribute(typeof(ParameterAttribute), false) as ParameterAttribute;
						sb.Append($"   {paramInfo.Name} ({paramInfo.ParameterType.Name})");
						if (paramAttr is not null) {
							sb.AppendLine($": {paramAttr.Description}");
						}
					}
				}
			}

			/// <summary>Process the command using the given arguments.</summary>
			/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
			/// <param name="arguments">The arguments.</param>
			/// <returns>A Result.</returns>
			internal Result Process(List<String> arguments) {
				Result returnCode = Result.Ok;
				Object callingObject;
				if (Handler.Attributes.HasFlag(MethodAttributes.Static)) {
					callingObject = null;
				} else {
					callingObject = Parent;
				}

				Object[] parameters = new Object[Parameters.Length];
				// Set any default parameters
				Int32 paramIdx = 0;
				Int32 lastOptionalParamIdx = Parameters.Length - 1;
				foreach (ParameterInfo parameter in Parameters) {
					if (parameter.IsOptional) {
						parameters[paramIdx] = parameter.DefaultValue;
						lastOptionalParamIdx = paramIdx - 1;
					}
					paramIdx++;
				}

				// Populate the parameters array
				paramIdx = 0;
				Boolean sawArrayParam = false;
				for (Int32 argIdx = 0; argIdx < Parameters.Length; argIdx++) {
					if (paramIdx >= parameters.Length) {
						throw new Exception($"Too many arguments for command \"{Handler.Name}\" (expects {Parameters.Length} arguments).");
					}
					// Try to parse the current argument as the expected type
					Type paramType = Parameters[paramIdx].ParameterType;
					Boolean arrayType = paramType.IsArray;
					Type elementType = paramType;
					if (arrayType) {
						// Get the element type
						elementType = paramType.GetElementType();
						sawArrayParam = true;
					}
					if ((elementType is not null) && DataType.Converters.TryGetValue(elementType, out TypeConverter? typeConverter)) {
						if (arrayType) {
							Array thisArray = Array.CreateInstance(elementType, arguments.Count - argIdx);
							for (Int32 arrayIdx = 0; argIdx < arguments.Count; argIdx++) {
								thisArray.SetValue(typeConverter.ParserFunc(arguments[argIdx]), arrayIdx++);
							}
							parameters[paramIdx] = thisArray;
						} else if (argIdx < arguments.Count) {
							// Argument has been provided. Otherwise, use the default value.
							parameters[paramIdx] = typeConverter.ParserFunc(arguments[argIdx]);
						}
					} else {
						throw new Exception($"The type \"{paramType.Name}\" is not currently supported for handler parameter arguments.");
					}
					paramIdx++;
				}
				if (paramIdx <= lastOptionalParamIdx) {
					if (sawArrayParam) {
						throw new Exception(
							$"The handler function \"{Handler.Name}\" has parameters after an array type.\r\n" +
							$"One array type is allowed only at the end of the parameter list."
						);
					}
					while (paramIdx < parameters.Length) {
						Parent.Logger.Note($"Handler parameter \"{Handler.Name}\" parameter #{paramIdx + 1} (\"{Parameters[paramIdx].Name}\") not optional and not provided.");
						returnCode = Result.FatalError;
						paramIdx++;
					}
				}
				if (returnCode != Result.FatalError) {
					// Execute the command
					try {
						if (Handler.Invoke(callingObject, parameters) is Result result) {
							if (Handler.ReturnType == typeof(Result)) {
								returnCode = result;
							}
						}
					} catch (Exception ex) {
						returnCode = Result.FatalError;
						Parent.Logger.Error(ex.ToString());
					}
				}
				return returnCode;
			}
		}
	}
}