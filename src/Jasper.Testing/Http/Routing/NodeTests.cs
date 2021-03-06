﻿using Jasper.Http.Routing;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Http.Routing
{
    public class NodeTests
    {
        [Fact]
        public void add_child_with_arg()
        {
            var node = new Node("a");
            var child = new Node("a/*");

            node.AddChild(child);

            node.ArgNodes.ShouldContain(child);

            node.NamedNodes.Count.ShouldBe(0);
        }

        [Fact]
        public void add_child_with_no_arg()
        {
            var node = new Node("a");
            var child = new Node("a/b");

            node.AddChild(child);

            node.NamedNodes["b"].ShouldBeSameAs(child);
        }

        [Fact]
        public void add_child_with_no_arg_2()
        {
            var node = new Node("a/*");
            var child = new Node("a/*/b");

            node.AddChild(child);

            node.NamedNodes["b"].ShouldBeSameAs(child);
        }

        [Fact]
        public void parent_route()
        {
            ShouldBeNullExtensions.ShouldBeNull(new Node(string.Empty).ParentRoute);

            new Node("a").ParentRoute.ShouldBe(string.Empty);

            new Node("a/*").ParentRoute.ShouldBe("a");
            new Node("a/*/b").ParentRoute.ShouldBe("a/*");
            new Node("a/*/b/c").ParentRoute.ShouldBe("a/*/b");
        }
    }
}
