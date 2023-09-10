﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using nadena.dev.ndmf;
using nadena.dev.ndmf.model;
using UnityEngine;

namespace nadena.dev.ndmf.fluent
{
    public delegate void InlinePass(BuildContext context);

    public sealed class DeclaringPass
    {
        private readonly SolverContext _solverContext;
        private readonly BuildPhase _phase;
        private readonly SolverPass _pass;
        
        internal DeclaringPass(SolverPass pass, SolverContext solverContext, BuildPhase phase)
        {
            _pass = pass;
            _solverContext = solverContext;
            _phase = phase;
        }
        
        public DeclaringPass BeforePlugin(string QualifiedName, string sourceFile = "", int sourceLine = 0)
        {
            _solverContext.Constraints.Add(new Constraint()
            {
                First = _pass.PassKey,
                Second = _solverContext.GetPluginPhases(_phase, QualifiedName).PluginStart.PassKey,
                Type = ConstraintType.WeakOrder,
                DeclaredFile = sourceFile,
                DeclaredLine = sourceLine,
            });

            return this;
        }

        public DeclaringPass BeforePlugin<T>(T plugin, string sourceFile = "", int sourceLine = 0) where T : fluent.Plugin<T>, new()
        {
            return BeforePlugin(plugin.QualifiedName, sourceFile, sourceLine);
        }
        
        public DeclaringPass BeforePass(string qualifiedName, string sourceFile = "", int sourceLine = 0)
        {
            _solverContext.Constraints.Add(new Constraint()
            {
                First = _pass.PassKey, 
                Second = new PassKey(qualifiedName),
                Type = ConstraintType.WeakOrder,
                DeclaredFile = sourceFile,
                DeclaredLine = sourceLine,
            });

            return this;
        }

        public DeclaringPass BeforePass<T>(T pass, string sourceFile = "", int sourceLine = 0) where T : Pass<T>, new()
        {
            return BeforePass(pass.QualifiedName, sourceFile, sourceLine);
        }
    } 
    
    public sealed partial class Sequence
    {
        private readonly IPlugin _plugin;
        private readonly string _sequenceBaseName;
        private readonly SolverContext _solverContext;
        private readonly BuildPhase _phase;
        private readonly SolverPass _sequenceStart, _sequenceEnd;

        private SolverPass _priorPass = null;
        
        private int inlinePassIndex = 0;

        
        internal Sequence(BuildPhase phase, SolverContext solverContext, IPlugin plugin, string sequenceBaseName)
        {
            _phase = phase;
            _solverContext = solverContext;
            _plugin = plugin;
            _sequenceBaseName = sequenceBaseName;

            var innate = _solverContext.GetPluginPhases(_phase, plugin.QualifiedName);
            _sequenceStart = CreateSequencingPass("<sequence start>", _ignored => { }, "", 0);
            _sequenceEnd = CreateSequencingPass("<sequence end>", _ignored => { }, "", 0);

            _solverContext.Constraints.Add(
                new Constraint()
                {
                    First = innate.PluginStart.PassKey,
                    Second = _sequenceStart.PassKey,
                    Type = ConstraintType.WeakOrder,
                }
            );
            _solverContext.Constraints.Add(
                new Constraint()
                {
                    First = _sequenceEnd.PassKey,
                    Second = innate.PluginEnd.PassKey,
                    Type = ConstraintType.WeakOrder,
                }
            );
            _solverContext.Constraints.Add(
                new Constraint()
                {
                    First = _sequenceStart.PassKey,
                    Second = _sequenceEnd.PassKey,
                    Type = ConstraintType.WeakOrder,
                }
            );

            _solverContext.AddPass(_sequenceStart);
            _solverContext.AddPass(_sequenceEnd);
        }
        
        private SolverPass CreateSequencingPass(string displayName, InlinePass callback, string sourceFile, int sourceLine)
        {
            var anonPass = new AnonymousPass(_sequenceBaseName + "/anonymous#" + inlinePassIndex++, displayName, callback);
            var pass = new SolverPass(_plugin, anonPass, _phase, _compatibleExtensions, _requiredExtensions);
            anonPass.IsPhantom = true;
            
            return pass;
        }
        
        public DeclaringPass Run<T>(T pass, [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLine = 0) where T : fluent.Pass<T>, new()
        {
            return InternalRun(pass, sourceFile, sourceLine);
        }

        private DeclaringPass InternalRun(IPass pass, string sourceFile, int sourceLine)
        {
            var solverPass = new SolverPass(_plugin, pass, _phase, _compatibleExtensions, _requiredExtensions);
            _solverContext.AddPass(solverPass);
            
            _solverContext.Constraints.Add(
                new Constraint()
                {
                    First = _sequenceStart.PassKey,
                    Second = solverPass.PassKey,
                    Type = ConstraintType.WeakOrder,
                }
            );
            
            _solverContext.Constraints.Add(
                new Constraint()
                {
                    First = solverPass.PassKey,
                    Second = _sequenceEnd.PassKey,
                    Type = ConstraintType.WeakOrder,
                }
            );

            if (_priorPass != null)
            {
                _solverContext.Constraints.Add(
                    new Constraint()
                    {
                        First = _priorPass.PassKey,
                        Second = solverPass.PassKey,
                        Type = ConstraintType.Sequence,
                    }
                );
            }

            _priorPass = solverPass;
            OnNewPass(solverPass);
            
            return new DeclaringPass(solverPass, _solverContext, _phase);
        }

        public DeclaringPass Run(string displayName, InlinePass inlinePass, [CallerFilePath] string sourceFile = "", [CallerLineNumber] int sourceLine = 0)
        {
            var anonPass = new AnonymousPass(_sequenceBaseName + "/anonymous#" + inlinePassIndex++, displayName, inlinePass);
            return InternalRun(anonPass, sourceFile, sourceLine);
        }
    }
}