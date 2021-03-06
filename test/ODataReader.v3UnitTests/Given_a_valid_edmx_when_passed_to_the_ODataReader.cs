﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Its.Recipes;
using ODataReader.v3;
using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;

namespace ODataReader.v3UnitTests
{
    public class Given_a_valid_edmx_when_passed_to_the_ODataReader
    {
        private ODataReader.v3.Reader reader;

        public Given_a_valid_edmx_when_passed_to_the_ODataReader()
        {
            reader = new Reader();
        }

        [Fact]
        public void It_returns_an_odcm_model()
        {
            var edmxElement =
                Any.Edmx(edmx => edmx.Add(
                    Any.DataServices(dataServices => dataServices.Add(
                        Any.Schema(schema => schema.Add(
                            Any.EntityContainer()))))));

            var serviceMetadata = new Dictionary<string, string>()
            {
                {"$metadata", edmxElement.ToString()}
            };
            var odcmModel = reader.GenerateOdcmModel(serviceMetadata);

            odcmModel.Should().NotBeNull("because a valid edmx should yield a valid model");
        }
    }
}
