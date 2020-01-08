using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BatchTDSAliaesUpdate.Tests
{
    [TestClass]
    public class UnitTest1
    {
        public UpdateService UpdateService { get; set; }

        [TestInitialize]
        public void TestMethod1()
        {
            UpdateService = new UpdateService();
        }

        [TestMethod]
        public void GetParentNewPath_ShouldReturnCorrectPathForLevel5()
        {
            var itemMapelist = new List<Item>
            {
                new Item
                {
                    NewName = "sitecore",
                    OldName = "sitecore",
                    Level = 1
                },
                new Item
                {
                    NewName = "1",
                    OldName = "content",
                    Level = 2
                },
                new Item
                {
                    NewName = "2",
                    OldName = "layout",
                    Level = 2
                },
                new Item
                {
                    NewName = "1",
                    OldName = "sitecore",
                    Level = 3
                },
                new Item
                {
                    NewName = "1",
                    OldName = "content",
                    Level = 3
                },
                new Item
                {
                    NewName = "1",
                    OldName = "content",
                    Level = 3
                },
                new Item
                {
                    NewName = "1",
                    OldName = "content",
                    Level = 3
                },
                new Item
                {
                    NewName = "1",
                    OldName = "content",
                    Level = 3
                },
                new Item
                {
                    NewName = "1",
                    OldName = "content",
                    Level = 3
                },

            };

            var currenItem = new Item()
            {
                Level = 5,
                OldName = "SXA",
                NewName = "1",
            };

            var parentNewPath = UpdateService.GetParentNewPath(itemMapelist, currenItem);
            Assert.AreEqual("", parentNewPath);
        }
    }
}
