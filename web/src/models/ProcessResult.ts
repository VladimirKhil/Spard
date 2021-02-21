/** Describes process result of type <T>. */
export default interface ProcessResult<T> {
	/** Result text. */
	result: T;
	/** Process execution time. */
	duration: string;
}
