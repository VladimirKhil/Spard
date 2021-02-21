/** Describes SPARD transformation request. */
export default interface TransformRequest {
	/** Transformation input. */
	input: string;
	/** SPARD transformation rules. */
	transform: string;
}
