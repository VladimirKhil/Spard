import SpardExampleBaseInfo from './SpardExampleBaseInfo';

/** Describes SPARD execution example base info. */
export default interface SpardExampleInfo extends SpardExampleBaseInfo {
	/** Input data. */
	input: string;
	/** SPARD transformation rules. */
	transform: string;
}
