﻿//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics.Contracts;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Threading;
//using System.Threading.Tasks;
//using LanguageExt;
//using LanguageExt.ClassInstances;
//using LanguageExt.TypeClasses;
//using static LanguageExt.Prelude;
//using static LanguageExt.TypeClass;

//namespace LanguageExt
//{
//    /// <summary>
//    /// Cons sequence
//    /// Represents a sequence of values in a similar way to IEnumerable, but without the
//    /// issues of multiple evaluation for key LINQ operators like Skip, Count, etc.
//    /// </summary>
//    /// <typeparam name="A">Type of the values in the sequence</typeparam>

//    public struct SeqAsync<A> :
//        IComparable<SeqAsync<A>>, IEquatable<SeqAsync<A>>
//    {
//        const int DefaultCapacity = 8;
//        const int HalfDefaultCapacity = DefaultCapacity >> 1;

//        /// <summary>
//        /// Empty sequence
//        /// </summary>
//        public static readonly SeqAsync<A> Empty = new SeqAsync<A>(Seq<A>.Empty);

//        /// <summary>
//        /// Internal representation of the sequence (SeqStrict|SeqLazy|SeqEmptyInternal)
//        /// </summary>
//        readonly Seq<Task<A>> value;

//        /// <summary>
//        /// Constructor from lazy sequence
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public SeqAsync(IEnumerable<Task<A>> ma) : this(new Seq<Task<A>>(ma)) { }

//        /// <summary>
//        /// Constructor
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        internal SeqAsync(Seq<Task<A>> value) =>
//            this.value = value;

//        /// <summary>
//        /// Reference version for use in pattern-matching
//        /// </summary>
//        [Pure]
//        public SeqAsyncCase<A> Case =>
//            IsEmpty
//                ? EmptyCaseAsync<A>.Default
//                : Tail.IsEmpty
//                    ? HeadCaseAsync<A>.New(Head)
//                    : HeadTailCaseAsync<A>.New(Head, Tail);

//        public void Deconstruct(out Task<A> head, out SeqAsync<A> tail)
//        {
//            head = Head;
//            tail = Tail;
//        }

//        /// <summary>
//        /// Head lens
//        /// </summary>
//        public static Lens<Seq<A>, A> head => Lens<Seq<A>, A>.New(
//            Get: la => la.IsEmpty ? throw new IndexOutOfRangeException() : la[0],
//            Set: a => la => la.IsEmpty ? throw new IndexOutOfRangeException() : a.Cons(la.Tail)
//            );

//        /// <summary>
//        /// Head or none lens
//        /// </summary>
//        public static Lens<Seq<A>, Option<A>> headOrNone => Lens<Seq<A>, Option<A>>.New(
//            Get: la => la.HeadOrNone(),
//            Set: a => la => la.IsEmpty || a.IsNone ? la : a.Value.Cons(la.Tail)
//            );

//        /// <summary>
//        /// Tail lens
//        /// </summary>
//        public static Lens<Seq<A>, Seq<A>> tail => Lens<Seq<A>, Seq<A>>.New(
//            Get: la => la.IsEmpty ? Seq<A>.Empty : la.Tail,
//            Set: a => la => la.IsEmpty ? a : la.Head.Cons(a)
//            );

//        /// <summary>
//        /// Last lens
//        /// </summary>
//        public static Lens<Seq<A>, A> last => Lens<Seq<A>, A>.New(
//            Get: la => la.IsEmpty ? throw new IndexOutOfRangeException() : la.Last,
//            Set: a => la => la.IsEmpty ? throw new IndexOutOfRangeException() : la.Take(la.Count - 1).Add(a)
//            );

//        /// <summary>
//        /// Last or none lens
//        /// </summary>
//        public static Lens<Seq<A>, Option<A>> lastOrNone => Lens<Seq<A>, Option<A>>.New(
//            Get: la => la.IsEmpty ? None : Some(la.Last),
//            Set: a => la => la.IsEmpty || a.IsNone ? la : la.Take(la.Count - 1).Add(a.Value)
//            );

//        /// <summary>
//        /// Lens map
//        /// </summary>
//        [Pure]
//        public static Lens<Seq<A>, Seq<B>> map<B>(Lens<A, B> lens) => Lens<Seq<A>, Seq<B>>.New(
//            Get: la => la.Map(lens.Get),
//            Set: lb => la => la.Zip(lb).Map(ab => lens.Set(ab.Item2, ab.Item1))
//            );

//        /// <summary>
//        /// Indexer
//        /// </summary>
//        public Task<A> this[int index]
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => value[index];
//        }

//        /// <summary>
//        /// Add an item to the end of the sequence
//        /// </summary>
//        /// <remarks>
//        /// Forces evaluation of the entire lazy sequence so the item 
//        /// can be appended
//        /// </remarks>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public SeqAsync<A> Add(Task<A> value) =>
//            new SeqAsync<A>(this.value.Add(value));

//        /// <summary>
//        /// Add a range of items to the end of the sequence
//        /// </summary>
//        /// <remarks>
//        /// Forces evaluation of the entire lazy sequence so the items
//        /// can be appended.  
//        /// </remarks>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public SeqAsync<A> Concat(IEnumerable<Task<A>> items) =>
//            new SeqAsync<A>(this.value.Concat(items));

//        /// <summary>
//        /// Add a range of items to the end of the sequence
//        /// </summary>
//        /// <remarks>
//        /// Forces evaluation of the entire lazy sequence so the items
//        /// can be appended.  
//        /// </remarks>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public SeqAsync<A> Concat(SeqAsync<A> rhs) =>
//            new SeqAsync<A>(value.Concat(rhs.value));

//        /// <summary>
//        /// Prepend an item to the sequence
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        internal SeqAsync<A> Cons(Task<A> value) =>
//            new SeqAsync<A>(this.value.Cons(value));

//        /// <summary>
//        /// Head item in the sequence.  NOTE:  If `IsEmpty` is true then Head 
//        /// is undefined.  Call HeadOrNone() if for maximum safety.
//        /// </summary>
//        public Task<A> Head
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => value.Head;
//        }

//        /// <summary>
//        /// Tail of the sequence
//        /// </summary>
//        public SeqAsync<A> Tail
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => new SeqAsync<A>(value.Tail);
//        }

//        /// <summary>
//        /// Get all items except the last one
//        /// </summary>
//        public SeqAsync<A> Init
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => new SeqAsync<A>(value.Init);
//        }

//        /// <summary>
//        /// Head of the sequence if this node isn't the empty node
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public OptionAsync<A> HeadOrNone() =>
//            IsEmpty
//                ? None
//                : SomeAsync(Head);

//        /// <summary>
//        /// Last item in sequence.  Throws if no items in sequence
//        /// </summary>
//        public Task<A> Last
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => value.Last;
//        }

//        /// <summary>
//        /// Last item in sequence.
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public OptionAsync<A> LastOrNone() =>
//            IsEmpty
//                ? OptionAsync<A>.None
//                : SomeAsync(Last);

//        /// <summary>
//        /// Last item in sequence.
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public EitherAsync<L, A> LastOrLeft<L>(L Left) =>
//            IsEmpty
//                ? LeftAsync<L, A>(Left)
//                : RightAsync<L, A>(Last);

//        /// <summary>
//        /// Last item in sequence.
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public EitherAsync<L, A> LastOrLeft<L>(Func<L> Left) =>
//            IsEmpty
//                ? LeftAsync<L, A>(Left())
//                : RightAsync<L, A>(Last);

//        // TODO when ValidationAsync exists

//        ///// <summary>
//        ///// Last item in sequence.
//        ///// </summary>
//        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//        //public Validation<F, A> LastOrInvalid<F>(F Fail) =>
//        //    IsEmpty
//        //        ? Validation<F, A>.Fail(Seq1(Fail))
//        //        : Validation<F, A>.Success(Last);

//        ///// <summary>
//        ///// Last item in sequence.
//        ///// </summary>
//        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//        //public Validation<F, A> LastOrInvalid<F>(Func<F> Fail) =>
//        //    IsEmpty
//        //        ? Validation<F, A>.Fail(Seq1(Fail()))
//        //        : Validation<F, A>.Success(Last);

//        ///// <summary>
//        ///// Last item in sequence.
//        ///// </summary>
//        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//        //public Validation<MonoidFail, F, A> LastOrInvalid<MonoidFail, F>(F Fail) where MonoidFail : struct, Monoid<F>, Eq<F> =>
//        //    IsEmpty
//        //        ? Validation<MonoidFail, F, A>.Fail(Fail)
//        //        : Validation<MonoidFail, F, A>.Success(Last);

//        ///// <summary>
//        ///// Last item in sequence.
//        ///// </summary>
//        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//        //public Validation<MonoidFail, F, A> LastOrInvalid<MonoidFail, F>(Func<F> Fail) where MonoidFail : struct, Monoid<F>, Eq<F> =>
//        //    IsEmpty
//        //        ? Validation<MonoidFail, F, A>.Fail(Fail())
//        //        : Validation<MonoidFail, F, A>.Success(Last);

//        ///// <summary>
//        ///// Head of the sequence if this node isn't the empty node or fail
//        ///// </summary>
//        ///// <typeparam name="Fail"></typeparam>
//        ///// <param name="fail">Fail case</param>
//        ///// <returns>Head of the sequence or fail</returns>
//        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//        //public Validation<Fail, A> HeadOrInvalid<Fail>(Fail fail) =>
//        //    IsEmpty
//        //        ? Fail<Fail, A>(fail)
//        //        : Success<Fail, A>(Head);

//        ///// <summary>
//        ///// Head of the sequence
//        ///// </summary>
//        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//        //public Validation<MonoidFail, Fail, A> HeadOrInvalid<MonoidFail, Fail>(Fail fail) where MonoidFail : struct, Monoid<Fail>, Eq<Fail> =>
//        //    IsEmpty
//        //        ? Fail<MonoidFail, Fail, A>(fail)
//        //        : Success<MonoidFail, Fail, A>(Head);

//        /// <summary>
//        /// Head of the sequence if this node isn't the empty node or left
//        /// </summary>
//        /// <typeparam name="L"></typeparam>
//        /// <param name="left">Left case</param>
//        /// <returns>Head of the sequence or left</returns>
//        public EitherAsync<L, A> HeadOrLeft<L>(L left) =>
//            IsEmpty
//                ? LeftAsync<L, A>(left)
//                : RightAsync<L, A>(Head);

//        /// <summary>
//        /// Head of the sequence if this node isn't the empty node
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public EitherAsync<L, A> HeadOrLeft<L>(Func<L> Left) =>
//            IsEmpty
//                ? LeftAsync<L, A>(Left())
//                : RightAsync<L, A>(Head);

//        /// <summary>
//        /// Returns true if the sequence is empty
//        /// </summary>
//        /// <remarks>
//        /// For lazy streams this will have to peek at the first 
//        /// item.  So, the first item will be consumed.
//        /// </summary>
//        public bool IsEmpty
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => value.IsEmpty;
//        }

//        /// <summary>
//        /// Returns the number of items in the sequence
//        /// </summary>
//        /// <returns>Number of items in the sequence</returns>
//        public int Count
//        {
//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            get => value.Count;
//        }

//        /// <summary>
//        /// Stream as an enumerable
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public IEnumerable<Task<A>> AsEnumerable() =>
//            value;

//        /// <summary>
//        /// Match empty sequence, or multi-item sequence
//        /// </summary>
//        /// <typeparam name="B">Return value type</typeparam>
//        /// <param name="Empty">Match for an empty list</param>
//        /// <param name="Tail">Match for a non-empty</param>
//        /// <returns>Result of match function invoked</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public async Task<B> Match<B>(
//            Func<B> Empty,
//            Func<A, SeqAsync<A>, B> Tail) =>
//            IsEmpty
//                ? Empty()
//                : Tail(await Head, this.Tail);

//        /// <summary>
//        /// Match empty sequence, or one item sequence, or multi-item sequence
//        /// </summary>
//        /// <typeparam name="B">Return value type</typeparam>
//        /// <param name="Empty">Match for an empty list</param>
//        /// <param name="Tail">Match for a non-empty</param>
//        /// <returns>Result of match function invoked</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public async Task<B> Match<B>(
//            Func<B> Empty,
//            Func<A, B> Head,
//            Func<A, SeqAsync<A>, B> Tail) =>
//            IsEmpty
//                ? Empty()
//                : this.Tail.IsEmpty
//                    ? Head(await this.Head)
//                    : Tail(await this.Head, this.Tail);

//        /// <summary>
//        /// Match empty sequence, or multi-item sequence
//        /// </summary>
//        /// <typeparam name="B">Return value type</typeparam>
//        /// <param name="Empty">Match for an empty list</param>
//        /// <param name="Sequence">Match for a non-empty</param>
//        /// <returns>Result of match function invoked</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public B Match<B>(
//            Func<B> Empty,
//            Func<SeqAsync<A>, B> Seq) =>
//            IsEmpty
//                ? Empty()
//                : Seq(this);

//        /// <summary>
//        /// Match empty sequence, or one item sequence, or multi-item sequence
//        /// </summary>
//        /// <typeparam name="B">Return value type</typeparam>
//        /// <param name="Empty">Match for an empty list</param>
//        /// <param name="Tail">Match for a non-empty</param>
//        /// <returns>Result of match function invoked</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public async Task<B> Match<B>(
//            Func<B> Empty,
//            Func<A, B> Head,
//            Func<SeqAsync<A>, B> Tail) =>
//            IsEmpty
//                ? Empty()
//                : this.Tail.IsEmpty
//                    ? Head(await this.Head)
//                    : Tail(this.Tail);

//        /// <summary>
//        /// Impure iteration of the bound values in the structure
//        /// </summary>
//        /// <returns>
//        /// Returns the original unmodified structure
//        /// </returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public SeqAsync<A> Do(Action<A> f)
//        {
//            this.Iter(f);
//            return this;
//        }

//        /// <summary>
//        /// Impure iteration of the bound values in the structure
//        /// </summary>
//        /// <returns>
//        /// Returns the original unmodified structure
//        /// </returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public Task<Unit> Iter(Action<A> f) =>
//            value.WindowIter(f);

//        /// <summary>
//        /// Impure iteration of the bound values in the structure
//        /// </summary>
//        /// <returns>
//        /// Returns the original unmodified structure
//        /// </returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public Task<Unit> Iter(int windowSize, Action<A> f) =>
//            value.WindowIter(windowSize, f);

//        /// <summary>
//        /// Map the sequence using the function provided
//        /// </summary>
//        /// <typeparam name="B"></typeparam>
//        /// <param name="f">Mapping function</param>
//        /// <returns>Mapped sequence</returns>
//        public SeqAsync<B> Map<B>(Func<A, B> f) =>
//            new SeqAsync<B>(value.Map(async a => f(await a)));

//        /// <summary>
//        /// Map the sequence using the function provided
//        /// </summary>
//        /// <typeparam name="B"></typeparam>
//        /// <param name="f">Mapping function</param>
//        /// <returns>Mapped sequence</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public SeqAsync<B> Select<B>(Func<A, B> f) =>
//            Map(f);

//        /// <summary>
//        /// Monadic bind (flatmap) of the sequence
//        /// </summary>
//        /// <typeparam name="B">Bound return value type</typeparam>
//        /// <param name="f">Bind function</param>
//        /// <returns>Flatmapped sequence</returns>
//        public SeqAsync<B> Bind<B>(Func<A, SeqAsync<B>> f)
//        {
//            Task<IEnumerable<Task<B>>> Yield2(Task<A> ta, Func<A, SeqAsync<B>> bnd) =>
//                ta.Map(bnd).Map(sb => (IEnumerable<Task<B>>)sb.value.Value);

//            IEnumerable<Task<IEnumerable<Task<B>>>> Yield(SeqAsync<A> ma, Func<A, SeqAsync<B>> bnd)
//            {
//                var ts = new List<Task<IEnumerable<Task<B>>>>();

//                foreach (var ta in ma)
//                {
//                    ts.Add(Yield2(ta, bnd));
//                }
//            }
//            return new Seq<B>(Yield(this, f));
//        }

//        /// <summary>
//        /// Monadic bind (flatmap) of the sequence
//        /// </summary>
//        /// <typeparam name="B">Bound return value type</typeparam>
//        /// <param name="bind">Bind function</param>
//        /// <returns>Flatmapped sequence</returns>
//        public Seq<C> SelectMany<B, C>(Func<A, Seq<B>> bind, Func<A, B, C> project)
//        {
//            IEnumerable<C> Yield(Seq<A> ma, Func<A, Seq<B>> bnd, Func<A, B, C> prj)
//            {
//                foreach (var a in ma)
//                {
//                    foreach (var b in bnd(a))
//                    {
//                        yield return prj(a, b);
//                    }
//                }
//            }
//            return new Seq<C>(Yield(this, bind, project));
//        }

//        /// <summary>
//        /// Filter the items in the sequence
//        /// </summary>
//        /// <param name="f">Predicate to apply to the items</param>
//        /// <returns>Filtered sequence</returns>
//        public Seq<A> Filter(Func<A, bool> f) =>
//            new Seq<A>(Value.Filter(f));

//        /// <summary>
//        /// Filter the items in the sequence
//        /// </summary>
//        /// <param name="f">Predicate to apply to the items</param>
//        /// <returns>Filtered sequence</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public Seq<A> Where(Func<A, bool> f) =>
//            Filter(f);

//        /// <summary>
//        /// Fold the sequence from the first item to the last
//        /// </summary>
//        /// <typeparam name="S">State type</typeparam>
//        /// <param name="state">Initial state</param>
//        /// <param name="f">Fold function</param>
//        /// <returns>Aggregated state</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public S Fold<S>(S state, Func<S, A, S> f) =>
//            Value.Fold(state, f);

//        /// <summary>
//        /// Fold the sequence from the last item to the first.  For 
//        /// sequences that are not lazy and are less than 5000 items
//        /// long, FoldBackRec is called instead, because it is faster.
//        /// </summary>
//        /// <typeparam name="S">State type</typeparam>
//        /// <param name="state">Initial state</param>
//        /// <param name="f">Fold function</param>
//        /// <returns>Aggregated state</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public S FoldBack<S>(S state, Func<S, A, S> f) =>
//            Value.FoldBack(state, f);

//        /// <summary>
//        /// Returns true if the supplied predicate returns true for any
//        /// item in the sequence.  False otherwise.
//        /// </summary>
//        /// <param name="f">Predicate to apply</param>
//        /// <returns>True if the supplied predicate returns true for any
//        /// item in the sequence.  False otherwise.</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public bool Exists(Func<A, bool> f) =>
//            Value.Exists(f);

//        /// <summary>
//        /// Returns true if the supplied predicate returns true for all
//        /// items in the sequence.  False otherwise.  If there is an 
//        /// empty sequence then true is returned.
//        /// </summary>
//        /// <param name="f">Predicate to apply</param>
//        /// <returns>True if the supplied predicate returns true for all
//        /// items in the sequence.  False otherwise.  If there is an 
//        /// empty sequence then true is returned.</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public bool ForAll(Func<A, bool> f) =>
//            Value.ForAll(f);

//        /// <summary>
//        /// Returns true if the sequence has items in it
//        /// </summary>
//        /// <returns>True if the sequence has items in it</returns>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public bool Any() =>
//            !IsEmpty;

//        /// <summary>
//        /// Get the hash code for all of the items in the sequence, or 0 if empty
//        /// </summary>
//        /// <returns></returns>
//        public override int GetHashCode() =>
//            hash = hash == 0
//                ? hash(this)
//                : hash;

//        /// <summary>
//        /// Format the collection as `[a, b, c, ...]`
//        /// The elipsis is used for collections over 50 items
//        /// To get a formatted string with all the items, use `ToFullString`
//        /// or `ToFullArrayString`.
//        /// </summary>
//        [Pure]
//        public override string ToString() =>
//            Value is SeqLazy<A>
//                ? CollectionFormat.ToShortArrayString(this)
//                : CollectionFormat.ToShortArrayString(this, Count);

//        /// <summary>
//        /// Format the collection as `a, b, c, ...`
//        /// </summary>
//        [Pure]
//        public string ToFullString(string separator = ", ") =>
//            CollectionFormat.ToFullString(this);

//        /// <summary>
//        /// Format the collection as `[a, b, c, ...]`
//        /// </summary>
//        [Pure]
//        public string ToFullArrayString(string separator = ", ") =>
//            CollectionFormat.ToFullArrayString(this);

//        /// <summary>
//        /// Append operator
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static Seq<A> operator +(Seq<A> x, Seq<A> y) =>
//            x.Concat(y);

//        /// <summary>
//        /// Ordering operator
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool operator >(Seq<A> x, Seq<A> y) =>
//            x.CompareTo(y) > 0;

//        /// <summary>
//        /// Ordering operator
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool operator >=(Seq<A> x, Seq<A> y) =>
//            x.CompareTo(y) >= 0;

//        /// <summary>
//        /// Ordering  operator
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool operator <(Seq<A> x, Seq<A> y) =>
//            x.CompareTo(y) < 0;

//        /// <summary>
//        /// Ordering  operator
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool operator <=(Seq<A> x, Seq<A> y) =>
//            x.CompareTo(y) <= 0;

//        /// <summary>
//        /// Equality operator
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool operator ==(Seq<A> x, Seq<A> y) =>
//            x.Equals(y);

//        /// <summary>
//        /// Non-equality operator
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static bool operator !=(Seq<A> x, Seq<A> y) =>
//            !(x == y);

//        /// <summary>
//        /// Equality test
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public override bool Equals(object obj) =>
//            obj is Seq<A> seq && Equals(seq);

//        /// <summary>
//        /// Equality test
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        [Obsolete(ISeqObsolete.Message)]
//        public bool Equals(ISeq<A> rhs) =>
//            Enumerable.SequenceEqual(this, rhs);

//        /// <summary>
//        /// Equality test
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public bool Equals(Seq<A> rhs) =>
//            Equals<EqDefault<A>>(rhs);

//        /// <summary>
//        /// Equality test
//        /// </summary>
//        public bool Equals<EqA>(Seq<A> rhs) where EqA : struct, Eq<A>
//        {
//            // Differing lengths?
//            if (Count != rhs.Count) return false;

//            // If the hash code has been calculated on both sides then 
//            // check for differences
//            if (hash != 0 && rhs.hash != 0 && hash != rhs.hash)
//            {
//                return false;
//            }

//            // Iterate through both sides
//            var iterA = GetEnumerator();
//            var iterB = rhs.GetEnumerator();
//            while (iterA.MoveNext() && iterB.MoveNext())
//            {
//                if (!default(EqA).Equals(iterA.Current, iterB.Current))
//                {
//                    return false;
//                }
//            }
//            return true;
//        }

//        /// <summary>
//        /// Skip count items
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public Seq<A> Skip(int amount) =>
//            amount < 1
//                ? this
//                : new Seq<A>(Value.Skip(amount));

//        /// <summary>
//        /// Take count items
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public Seq<A> Take(int amount) =>
//            amount < 1
//                ? Empty
//                : new Seq<A>(Value.Take(amount));

//        /// <summary>
//        /// Iterate the sequence, yielding items if they match the predicate 
//        /// provided, and stopping as soon as one doesn't
//        /// </summary>
//        /// <returns>A new sequence with the first items that match the 
//        /// predicate</returns>
//        public Seq<A> TakeWhile(Func<A, bool> pred)
//        {
//            var data = new A[DefaultCapacity];
//            var index = HalfDefaultCapacity;

//            foreach (var item in this)
//            {
//                if (pred(item))
//                {
//                    if (index == data.Length)
//                    {
//                        var ndata = new A[Math.Max(1, data.Length << 1)];
//                        System.Array.Copy(data, ndata, data.Length);
//                        data = ndata;
//                    }

//                    data[index] = item;
//                    index++;
//                }
//            }
//            return index == HalfDefaultCapacity
//                ? Empty
//                : new Seq<A>(new SeqStrict<A>(data, HalfDefaultCapacity, index, 0, 0));
//        }

//        /// <summary>
//        /// Iterate the sequence, yielding items if they match the predicate 
//        /// provided, and stopping as soon as one doesn't.  An index value is 
//        /// also provided to the predicate function.
//        /// </summary>
//        /// <returns>A new sequence with the first items that match the 
//        /// predicate</returns>
//        public Seq<A> TakeWhile(Func<A, int, bool> pred)
//        {
//            var data = new A[DefaultCapacity];
//            var index = HalfDefaultCapacity;

//            foreach (var item in this)
//            {
//                if (pred(item, index))
//                {
//                    if (index == data.Length)
//                    {
//                        var ndata = new A[Math.Max(1, data.Length << 1)];
//                        System.Array.Copy(data, ndata, data.Length);
//                        data = ndata;
//                    }

//                    data[index] = item;
//                    index++;
//                }
//            }
//            return index == HalfDefaultCapacity
//                ? Empty
//                : new Seq<A>(new SeqStrict<A>(data, HalfDefaultCapacity, index, 0, 0));
//        }

//        /// <summary>
//        /// Compare to another sequence
//        /// </summary>
//        [Obsolete(ISeqObsolete.Message)]
//        public int CompareTo(ISeq<A> rhs) =>
//            CompareTo<OrdDefault<A>>(rhs);

//        /// <summary>
//        /// Compare to another sequence
//        /// </summary>
//        [Obsolete(ISeqObsolete.Message)]
//        public int CompareTo<OrdA>(ISeq<A> rhs) where OrdA : struct, Ord<A>
//        {
//            if (rhs == null) return 1;

//            // Differing lengths?
//            var cmp = Count.CompareTo(rhs.Count);
//            if (cmp != 0) return cmp;

//            // Iterate through both sides
//            var iterA = GetEnumerator();
//            var iterB = rhs.GetEnumerator();
//            while (iterA.MoveNext() && iterB.MoveNext())
//            {
//                cmp = default(OrdA).Compare(iterA.Current, iterB.Current);
//                if (cmp != 0) return cmp;
//            }
//            return 0;
//        }

//        /// <summary>
//        /// Compare to another sequence
//        /// </summary>
//        public int CompareTo(Seq<A> rhs) =>
//            CompareTo<OrdDefault<A>>(rhs);

//        /// <summary>
//        /// Compare to another sequence
//        /// </summary>
//        public int CompareTo<OrdA>(Seq<A> rhs) where OrdA : struct, Ord<A>
//        {
//            if (rhs == null) return 1;

//            // Differing lengths?
//            var cmp = Count.CompareTo(rhs.Count);
//            if (cmp != 0) return cmp;

//            // Iterate through both sides
//            var iterA = GetEnumerator();
//            var iterB = rhs.GetEnumerator();
//            while (iterA.MoveNext() && iterB.MoveNext())
//            {
//                cmp = default(OrdA).Compare(iterA.Current, iterB.Current);
//                if (cmp != 0) return cmp;
//            }
//            return 0;
//        }

//        /// <summary>
//        /// Force all items lazy to stream
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public SeqAsync<A> Strict() =>
//            new SeqAsync<A>(value.Strict());

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public IEnumerator<Task<A>> GetEnumerator() =>
//            value.GetEnumerator();

//        /// <summary>
//        /// Implicit conversion from an untyped empty list
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static implicit operator Seq<A>(SeqEmpty _) =>
//            Empty;

//        [Pure]
//        public Seq<B> Cast<B>()
//        {
//            IEnumerable<B> Yield(Seq<A> ma)
//            {
//                foreach (object item in ma)
//                {
//                    yield return (B)item;
//                }
//            }

//            return Value is IEnumerable<B> mb
//                ? new Seq<B>(mb)
//                : new Seq<B>(Yield(this));
//        }
//    }
//}