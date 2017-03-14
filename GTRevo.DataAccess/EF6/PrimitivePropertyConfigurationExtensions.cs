﻿using System;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace GTRevo.DataAccess.EF6
{
    public static class PrimitivePropertyConfigurationExtensions
    {
        public static PrimitivePropertyConfiguration HasInheritedColumnName<TBase>(this PrimitivePropertyConfiguration config, string name)
        {
            //var internalConfig = typeof(PrimitivePropertyConfiguration).GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(config); //System.Data.Entity.ModelConfiguration.Configuration.Properties.Primitive.PrimitivePropertyConfiguration
            //var typeConfiguration = internalConfig.GetType().GetProperty("TypeConfiguration", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(internalConfig); //StructuralTypeConfiguration

            //Type baseType = (Type)typeConfiguration.GetType().GetField("_clrType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(typeConfiguration);

            Type baseType = typeof(TBase);
            return config.HasColumnAnnotation("InheritedColumnName", new InheritedColumnNameAnnotation(baseType, name));
        }
    }
}
