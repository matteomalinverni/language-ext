﻿using System;

namespace LanguageExt.HKT;



/// <summary>
/// Functor 
/// </summary>
/// <typeparam name="F">Functor trait type</typeparam>
/// <typeparam name="A">Bound value type</typeparam>
public interface Applicative<F> : Functor<F>
    where F : Applicative<F>
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Abstract members
    //
    
    public static abstract Applicative<F, A> Pure<A>(A value);

    public static abstract Applicative<F, B> Apply<A, B>(Applicative<F, Transducer<A, B>> mf, Applicative<F, A> ma);
    
    public static abstract Applicative<F, B> Action<A, B>(Applicative<F, A> ma, Applicative<F, B> mb);
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Default implementations
    //

    public static virtual Applicative<F, B> Apply<A, B>(Applicative<F, Func<A, B>> mf, Applicative<F, A> ma) =>
        F.Apply(F.Map(mf, Prelude.lift), ma);

    public static virtual Applicative<F, C> Apply<A, B, C>(Applicative<F, Func<A, B, C>> mf, Applicative<F, A> ma, Applicative<F, B> mb) =>
        F.Apply(F.Apply(F.Map(mf, Prelude.lift), ma), mb);

    public static virtual Applicative<F, C> Apply<A, B, C>(Applicative<F, Func<A, Func<B, C>>> mf, Applicative<F, A> ma, Applicative<F, B> mb) =>
        F.Apply(F.Apply(F.Map(mf, Prelude.lift), ma), mb);

    public static virtual Applicative<F, B> Map<A, B>(Applicative<F, A> ma, Transducer<A, B> f) =>
        (Applicative<F, B>)F.Map(f, ma);

    public static virtual Applicative<F, B> Map<A, B>(Applicative<F, A> ma, Func<A, B> f) =>
        (Applicative<F, B>)F.Map(f, ma);
}