/** Describes SPARD table transform result. */
export default interface TransformTableResult {
	/** Result text. */
	result: string;
	/** Does standard SPARD transformer generate the same result. */
	isStandardResultTheSame: boolean;
	/** Time for parsing expression and building standard transformer. */
	parseDuration: string;
	/** Time for building table transformer. */
	tableBuildDuration: string;
	/** Standard transform duration. */
	standardTransformDuration: string;
	/** Table transform duration. */
	tableTransformDuration: string;
}
