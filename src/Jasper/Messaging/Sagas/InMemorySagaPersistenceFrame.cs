﻿using System;
using System.Collections.Generic;
using Jasper.Messaging.Durability;
using LamarCompiler;
using LamarCompiler.Frames;
using LamarCompiler.Model;

namespace Jasper.Messaging.Sagas
{
    public class InMemorySagaPersistenceFrame : AsyncFrame
    {
        private readonly SagaStateExistence _existence;
        private readonly Variable _sagaId;
        private Variable _context;
        private Variable _persistor;

        public InMemorySagaPersistenceFrame(Type documentType, Variable sagaId, SagaStateExistence existence)
        {
            _sagaId = sagaId;
            _existence = existence;
            Document = new Variable(documentType, this);

            Persistor = new Variable(typeof(InMemoryEnvelopeTransaction), this);
        }

        public Variable Persistor { get; }

        public Variable Document { get; }

        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            writer.Write($"var {Persistor.Usage} = new {typeof(InMemoryEnvelopeTransaction).FullNameInCode()}();");
            writer.Write($"await {_context.Usage}.{nameof(IMessageContext.EnlistInTransaction)}({Persistor.Usage});");


            if (_existence == SagaStateExistence.Existing)
                writer.Write(
                    $"var {Document.Usage} = {_persistor.Usage}.{nameof(InMemorySagaPersistor.Load)}<{Document.VariableType.FullNameInCode()}>({_sagaId.Usage});");

            Next?.GenerateCode(method, writer);
        }

        public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
        {
            _context = chain.FindVariable(typeof(IMessageContext));
            yield return _context;

            yield return _sagaId;

            _persistor = chain.FindVariable(typeof(InMemorySagaPersistor));

            yield return _persistor;
        }
    }
}
