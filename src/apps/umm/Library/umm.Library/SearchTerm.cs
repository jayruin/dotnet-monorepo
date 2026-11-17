using System.Text.Json.Serialization;

namespace umm.Library;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "operator")]
[JsonDerivedType(typeof(ConstantSearchTerm), "constant")]
[JsonDerivedType(typeof(MatchSearchTerm), "match")]
[JsonDerivedType(typeof(EqualsSearchTerm), "equals")]
[JsonDerivedType(typeof(ContainsSearchTerm), "contains")]
[JsonDerivedType(typeof(HasSearchTerm), "has")]
[JsonDerivedType(typeof(NotSearchTerm), "not")]
[JsonDerivedType(typeof(AndSearchTerm), "and")]
[JsonDerivedType(typeof(OrSearchTerm), "or")]
public closed class SearchTerm;
