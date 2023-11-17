﻿#nullable enable
namespace LanguageExt.Transducers;

/// <summary>
/// Identity transducer, simply passes the value through 
/// </summary>
record IdentityTransducer<A> : Transducer<A, A>
{
    public static readonly Transducer<A, A> Default = new IdentityTransducer<A>();

    public Reducer<A, S> Transform<S>(Reducer<A, S> reduce) =>
        reduce;
                
    public override string ToString() =>  
        "identity";

    public Transducer<A, A> Morphism =>
        this;
}
