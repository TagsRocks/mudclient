﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectedMonsterParameterDescription.cs" company="Adamand MUD">
//   Copyright (c) Adamant MUD
// </copyright>
// <summary>
//   Defines the SelectedMonsterParameterDescription type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Adan.Client.Plugins.GroupWidget
{
    using System.Collections.Generic;

    using Common.Model;
    using Common.Plugins;
    using Common.ViewModel;

    using CSLib.Net.Annotations;
    using CSLib.Net.Diagnostics;

    using ViewModel;

    /// <summary>
    /// A description of "selected monster" parameter.
    /// </summary>
    public class SelectedMonsterParameterDescription : ParameterDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedMonsterParameterDescription"/> class.
        /// </summary>
        /// <param name="parameterDescriptions">The parameter descriptions.</param>
        public SelectedMonsterParameterDescription([NotNull] IEnumerable<ParameterDescription> parameterDescriptions)
            : base("Selected monster", parameterDescriptions)
        {
            Assert.ArgumentNotNull(parameterDescriptions, "parameterDescriptions");
        }

        /// <summary>
        /// Creates the parameter.
        /// </summary>
        /// <returns>Created parameter.</returns>
        public override ActionParameterBase CreateParameter()
        {
            return new SelectedMonsterParameter();
        }

        /// <summary>
        /// Creates the parameter view model.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        /// Created parameter view model.
        /// </returns>
        public override ActionParameterViewModelBase CreateParameterViewModel(ActionParameterBase parameter)
        {
            Assert.ArgumentNotNull(parameter, "parameter");

            return new SelectedMonsterParameterViewModel(parameter, this, ParameterDescriptions);
        }
    }
}
