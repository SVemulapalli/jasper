﻿using System;
using System.Collections.Generic;
using Jasper.Configuration;
using Jasper.Messaging.Configuration;
using Jasper.Messaging.Model;
using LamarCompiler;
using LamarCompiler.Frames;
using LamarCompiler.Model;

namespace Jasper.Testing.FakeStoreTypes
{
    public class GenericFakeTransactionAttribute : ModifyChainAttribute
    {
        public override void Modify(IChain chain, JasperGenerationRules rules)
        {
            chain.Middleware.Add(new FakeTransaction());
        }
    }

    public class FakeTransactionAttribute : ModifyHandlerChainAttribute
    {
        public override void Modify(HandlerChain chain, JasperGenerationRules rules)
        {
            chain.Middleware.Add(new FakeTransaction());
        }
    }

    public class FakeTransaction : Frame
    {
        private readonly Variable _session;
        private Variable _store;

        public FakeTransaction() : base(false)
        {
            _session = new Variable(typeof(IFakeSession), "session", this);
        }

        public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
        {
            _store = chain.FindVariable(typeof(IFakeStore));
            yield return _store;
        }

        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            writer.Write($"BLOCK:using (var {_session.Usage} = {_store.Usage}.OpenSession())");
            Next?.GenerateCode(method, writer);
            writer.Write($"{_session.Usage}.{nameof(IFakeSession.SaveChanges)}();");
            writer.FinishBlock();
        }
    }

    public interface IFakeStore
    {
        IFakeSession OpenSession();
    }

    public class Tracking
    {
        public bool CalledSaveChanges;
        public bool DisposedTheSession;
        public bool OpenedSession;
    }

    public class FakeStore : IFakeStore
    {
        private readonly Tracking _tracking;

        public FakeStore(Tracking tracking)
        {
            _tracking = tracking;
        }

        public IFakeSession OpenSession()
        {
            _tracking.OpenedSession = true;
            return new FakeSession(_tracking);
        }
    }

    public interface IFakeSession : IDisposable
    {
        void SaveChanges();
    }

    public class FakeSession : IFakeSession
    {
        private readonly Tracking _tracking;

        public FakeSession(Tracking tracking)
        {
            _tracking = tracking;
        }

        public void Dispose()
        {
            _tracking.DisposedTheSession = true;
        }

        public void SaveChanges()
        {
            _tracking.CalledSaveChanges = true;
        }
    }
}
