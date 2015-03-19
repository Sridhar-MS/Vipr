// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Annotations;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Expressions;
using Microsoft.OData.Edm.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Vipr.Core;
using Vipr.Core.CodeModel;
using Vipr.Core.CodeModel.Vocabularies.Capabilities;

namespace ODataReader.v4
{
    internal class ODataCapabilitiesReader
    {
        private IEdmModel _capabilitiesModel;
        private IEdmModel _serviceModel;
        
        const string CapabilitiesInsertRestrictions = "Org.OData.Capabilities.V1.InsertRestrictions";
        const string CapabilitiesUpdateRestrictions = "Org.OData.Capabilities.V1.UpdateRestrictions";
        const string CapabilitiesDeleteRestrictions = "Org.OData.Capabilities.V1.DeleteRestrictions";
        const string CapabilitiesExpandRestrictions = "Org.OData.Capabilities.V1.ExpandRestrictions";

        protected Dictionary<IEdmValueTerm, EntitySetAnnotationParser> WellKnownRestrictionTerms;

        protected delegate void EntitySetAnnotationParser(OdcmProperty odcmEntitySet, IEdmValueAnnotation annotation);

        protected Dictionary<OdcmProperty, List<OdcmCapability>> PropertyCache =
                new Dictionary<OdcmProperty, List<OdcmCapability>>();
        
        public ODataCapabilitiesReader(IEdmModel serviceModel)
        {
            using(var reader = new StringReader(Properties.Resources.CapabilitiesVocabularies))
            {
                IEnumerable<EdmError> errors;
                if (!CsdlReader.TryParse(new[] { XmlReader.Create(reader) }, out _capabilitiesModel, out errors))
                {
                    throw new InvalidOperationException("Could not load capabilities vocabulary from resources");
                }
            }

            _serviceModel = serviceModel;
            WellKnownRestrictionTerms = new Dictionary<IEdmValueTerm, EntitySetAnnotationParser>()
            {
                {_capabilitiesModel.FindDeclaredValueTerm(CapabilitiesInsertRestrictions), SetCapabilitiesForEntitySetAndNavigationProperties},
                {_capabilitiesModel.FindDeclaredValueTerm(CapabilitiesUpdateRestrictions), SetCapabilitiesForEntitySetAndNavigationProperties},
                {_capabilitiesModel.FindDeclaredValueTerm(CapabilitiesDeleteRestrictions), SetCapabilitiesForEntitySetAndNavigationProperties},
                {_capabilitiesModel.FindDeclaredValueTerm(CapabilitiesExpandRestrictions), SetCapabilitiesForEntitySetAndNavigationProperties},
            };
        }

        protected void CreateProjectionsForProperties()
        {
            foreach (var propertyPair in PropertyCache)
            {
                var property = propertyPair.Key;
                var capabilities = propertyPair.Value;
                var propertyType = property.Projection.Type;
                property.Projection = propertyType.GetProjection(capabilities);
            }
        }

        public virtual void SetCapabilitiesForEntityContainer(OdcmServiceClass odcmServiceClass, IEdmEntityContainer edmEntityContainer)
        {
            //TODO: Add Capability Annotation support for EntityContainers
        }

        public virtual void SetCapabilitiesForEntitySet(OdcmProperty odcmEntitySet, IEdmEntitySet edmEntitySet)
        {
            if (odcmEntitySet == null) throw new ArgumentNullException("odcmEntitySet");
            if (edmEntitySet == null) throw new ArgumentNullException("edmEntitySet");
            
            foreach (var restriction in WellKnownRestrictionTerms.Keys)
            {
                IEdmValueAnnotation annotation = FindVocabularyAnnotation(edmEntitySet, restriction);
                if (annotation != null)
                {
                    WellKnownRestrictionTerms[restriction](odcmEntitySet, annotation);
                }
            }

            CreateProjectionsForProperties();
        }

        private void SetCapabilitiesForEntitySetAndNavigationProperties(OdcmProperty odcmEntitySet, IEdmValueAnnotation annotation)
        {
            var recordExpression = (IEdmRecordExpression)annotation.Value;
            var boolVal = GetBooleanValue(recordExpression);
            List<OdcmProperty> navigationProperties = GetNavigationProperties(recordExpression,
                odcmEntitySet.Type as OdcmClass);

            List<OdcmCapability> capabilities;
            if (!PropertyCache.TryGetValue(odcmEntitySet, out capabilities))
            {
                capabilities = new List<OdcmCapability>();
                PropertyCache.Add(odcmEntitySet, capabilities);
            }

            capabilities.Add(GetBooleanCapabiltity(boolVal, annotation.Term.FullName()));

            foreach (var navigationProperty in navigationProperties)
            {
                if (!PropertyCache.TryGetValue(navigationProperty, out capabilities))
                {
                    capabilities = new List<OdcmCapability>();
                    PropertyCache.Add(navigationProperty, capabilities);
                }

                capabilities.Add(GetBooleanCapabiltity(false, annotation.Term.FullName()));
            }
        }

        private OdcmCapability GetBooleanCapabiltity(bool boolVal, string restriction)
        {
            OdcmCapability capability = null;
            Type capabilityType = null;
            switch (restriction)
            {
                case CapabilitiesInsertRestrictions:
                    capabilityType = typeof(OdcmInsertCapability);
                    break;
                case CapabilitiesUpdateRestrictions:
                    capabilityType = typeof(OdcmUpdateCapability);
                    break;
                case CapabilitiesDeleteRestrictions:
                    capabilityType = typeof(OdcmDeleteCapability);
                    break;
                case CapabilitiesExpandRestrictions:
                    capabilityType = typeof(OdcmExpandCapability);
                    break;
            }

            if (capabilityType != null)
            {
                capability = (OdcmCapability)Activator.CreateInstance(capabilityType);
                var booleanProperty = capabilityType.GetProperties().Single(p => p.PropertyType == typeof(bool));
                booleanProperty.SetValue(capability, boolVal);
            }

            return capability;
        }

        private bool GetBooleanValue(IEdmRecordExpression recordExpression)
        {
            var booleanExpression = (IEdmBooleanConstantExpression)recordExpression.Properties.Single(p => p.Value is IEdmBooleanConstantExpression).Value;
            return booleanExpression.Value;
        }

        private List<OdcmProperty> GetNavigationProperties(IEdmRecordExpression recordExpression, OdcmClass containerClass)
        {
            var properties = new List<OdcmProperty>();
            var collectionExpression = (IEdmCollectionExpression)recordExpression.Properties.Single(p => p.Value is IEdmCollectionExpression).Value;
            foreach (IEdmPathExpression pathExpression in collectionExpression.Elements)
            {
                var pathBuilder = new StringBuilder();
                foreach (var path in pathExpression.Path)
                {
                    pathBuilder.AppendFormat("{0}.", path);
                }

                pathBuilder.Remove(pathBuilder.Length - 1, 1);

                OdcmProperty navProperty;
                if (!containerClass.TryFindProperty(pathBuilder.ToString(), out navProperty))
                {
                    throw new InvalidOperationException();
                }
                properties.Add(navProperty);
            }

            return properties;
        }

        private IEdmValueAnnotation FindVocabularyAnnotation(IEdmVocabularyAnnotatable target, IEdmValueTerm term)
        {
            var result = default(IEdmValueAnnotation);

            var annotations = _serviceModel.FindVocabularyAnnotations(target);
            if (annotations != null)
            {
                var annotation = annotations.FirstOrDefault(a => a.Term.Namespace == term.Namespace && a.Term.Name == term.Name);
                result = (IEdmValueAnnotation)annotation;
            }

            return result;
        }

        /*internal static void GetCapabilitiesForEntitySet(OdcmProperty odcmProperty, IEdmModel edmModel, IEdmEntitySet entitySet)
        {
            if (odcmProperty == null) throw new ArgumentNullException("odcmProperty");
            if (edmModel == null) throw new ArgumentNullException("edmModel");
            if (entitySet == null) throw new ArgumentNullException("entitySet");

            GetInsertRestrictions(odcmProperty, edmModel, entitySet);
            GetUpdateRestrictions(odcmProperty, edmModel, entitySet);
            GetDeleteRestrictions(odcmProperty, edmModel, entitySet);
            GetExpandRestrictions(odcmProperty, edmModel, entitySet);
        }

        private static void GetInsertRestrictions(OdcmProperty odcmProperty, IEdmModel model, IEdmEntitySet entitySet)
        {
            IEdmValueAnnotation insertAnnotation = FindVocabularyAnnotation(model, entitySet, s_insertRestrictionsTerm);
            if(insertAnnotation == null)
            {
                return;
            }
            
            bool insertable;
            IEnumerable<string> nonInsertableNavigationProperties;
            GetBooleanAndPathCollection(insertAnnotation, "Insertable", "NonInsertableNavigationProperties", out insertable, out nonInsertableNavigationProperties);
            odcmProperty.Projection.EnsureCapabilities().Insertable = insertable;

            foreach (var propertyPath in nonInsertableNavigationProperties)
            {
                OdcmProperty navProperty;
                if (!(odcmProperty.Type as OdcmClass).TryFindProperty(propertyPath, out navProperty))
                {
                    throw new InvalidOperationException();
                }
                navProperty.Projection.EnsureCapabilities().Insertable = false;
            }
        }

        private static void GetUpdateRestrictions(OdcmProperty odcmProperty, IEdmModel model, IEdmEntitySet entitySet)
        {
            IEdmValueAnnotation updateAnnotation = FindVocabularyAnnotation(model, entitySet, s_updateRestrictionsTerm);
            if (updateAnnotation == null)
            {
                return;
            }

            bool updatable;
            IEnumerable<string> nonupdatableNavigationProperties;
            GetBooleanAndPathCollection(updateAnnotation, "Updatable", "NonUpdatableNavigationProperties", out updatable, out nonupdatableNavigationProperties);
            odcmProperty.Projection.EnsureCapabilities().Updateable = updatable;

            foreach (var propertyPath in nonupdatableNavigationProperties)
            {
                OdcmProperty navProperty;
                if (!(odcmProperty.Type as OdcmClass).TryFindProperty(propertyPath, out navProperty))
                {
                    throw new InvalidOperationException();
                }
                navProperty.Projection.EnsureCapabilities().Updateable = false;
            }
        }

        private static void GetDeleteRestrictions(OdcmProperty odcmProperty, IEdmModel model, IEdmEntitySet entitySet)
        {
            IEdmValueAnnotation deleteAnnotation = FindVocabularyAnnotation(model, entitySet, s_deleteRestrictionsTerm);
            if (deleteAnnotation == null)
            {
                return;
            }

            bool deletable;
            IEnumerable<string> nonDeletableNavigationProperties;
            GetBooleanAndPathCollection(deleteAnnotation, "Deletable", "NonDeletableNavigationProperties", out deletable, out nonDeletableNavigationProperties);
            odcmProperty.Projection.EnsureCapabilities().Deleteable = deletable;

            foreach (var propertyPath in nonDeletableNavigationProperties)
            {
                OdcmProperty navProperty;
                if (!(odcmProperty.Type as OdcmClass).TryFindProperty(propertyPath, out navProperty))
                {
                    throw new InvalidOperationException();
                }
                navProperty.Projection.EnsureCapabilities().Deleteable = false;
            }
        }

        private static void GetExpandRestrictions(OdcmProperty odcmProperty, IEdmModel model, IEdmEntitySet entitySet)
        {
            IEdmValueAnnotation expandAnnotation = FindVocabularyAnnotation(model, entitySet, s_expandRestrictionsTerm);
            if (expandAnnotation == null)
            {
                return;
            }

            bool expandable;
            IEnumerable<string> nonExpandableProperties;
            GetBooleanAndPathCollection(expandAnnotation, "Expandable", "NonExpandableProperties", out expandable, out nonExpandableProperties);
            odcmProperty.Projection.EnsureCapabilities().Expandable = expandable;

            foreach (var propertyPath in nonExpandableProperties)
            {
                OdcmProperty navProperty;
                if (!(odcmProperty.Type as OdcmClass).TryFindProperty(propertyPath, out navProperty))
                {
                    throw new InvalidOperationException();
                }
                navProperty.Projection.EnsureCapabilities().Expandable = false;
            }
        }

        private static void GetBooleanAndPathCollection(IEdmValueAnnotation annotation, string booleanPropertyName, string pathsPropertyName, out bool boolean, out IEnumerable<string> paths)
        {
            paths = new List<string>();            

            var recordExpression = (IEdmRecordExpression)annotation.Value;
            var booleanExpression = (IEdmBooleanConstantExpression)recordExpression.Properties.Single(p => p.Name == booleanPropertyName).Value;
            var collectionExpression = (IEdmCollectionExpression)recordExpression.Properties.Single(p => p.Name == pathsPropertyName).Value;

            foreach (IEdmPathExpression pathExpression in collectionExpression.Elements)
            {
                var pathBuilder = new StringBuilder();
                foreach (var path in pathExpression.Path)
                {
                    pathBuilder.AppendFormat("{0}.", path);
                }

                pathBuilder.Remove(pathBuilder.Length - 1, 1);

                ((List<string>)paths).Add(pathBuilder.ToString());
            }

            boolean = booleanExpression.Value;
        }

        private static IEdmValueAnnotation FindVocabularyAnnotation(IEdmModel model, IEdmVocabularyAnnotatable target, IEdmValueTerm term)
        {
            var result = default(IEdmValueAnnotation);

            var annotations = model.FindVocabularyAnnotations(target);
            if (annotations != null)
            {
                var annotation = annotations.FirstOrDefault(a => a.Term.Namespace == term.Namespace && a.Term.Name == term.Name);
                result = (IEdmValueAnnotation)annotation;
            }

            return result;
        }*/
    }
}
