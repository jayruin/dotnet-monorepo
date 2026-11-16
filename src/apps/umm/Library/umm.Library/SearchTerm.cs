using System.Text.Json.Serialization;

namespace umm.Library;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "operator")]
[JsonDerivedType(typeof(SearchConstantTerm), "constant")]
[JsonDerivedType(typeof(SearchMatchTerm), "match")]
[JsonDerivedType(typeof(SearchEqualsTerm), "equals")]
[JsonDerivedType(typeof(SearchContainsTerm), "contains")]
[JsonDerivedType(typeof(SearchNotTerm), "not")]
[JsonDerivedType(typeof(SearchAndTerm), "and")]
[JsonDerivedType(typeof(SearchOrTerm), "or")]
public closed class SearchTerm;
