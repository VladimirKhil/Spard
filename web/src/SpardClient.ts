import SpardClientOptions from './SpardClientOptions';
import SpardExampleBaseInfo from './models/SpardExampleBaseInfo';
import SpardExampleInfo from './models/SpardExampleInfo';
import TransformRequest from './models/TransformRequest';
import ProcessResult from './models/ProcessResult';
import TransformTableResult from './models/TransformTableResult';

import fetch from 'cross-fetch';

const JSON_HEADERS: Record<string, string> = {
	'Content-Type': 'application/json'
};

/** Defines SPARD service client. */
export default class SpardClient {
	/**
	 * Initializes a new instance of SpardClient class.
	 * @param options Client options.
	 */
	constructor(private options: SpardClientOptions) { }

	/** Gets all examples. */
	async getExamplesAsync(): Promise<SpardExampleBaseInfo[]> {
		return this.getAsync<SpardExampleBaseInfo[]>('examples');
	}

	/**
	 * Gets example by Id.
	 * @param id Example Id.
	 */
	async getExampleAsync(id: number): Promise<SpardExampleInfo> {
		return this.getAsync<SpardExampleInfo>(`examples/${id}`);
	}

	/**
	 * Transforms input using SPARD rules.
	 * @param transformRequest Transformation request.
	 * @returns Transformation result.
	 */
	async transformAsync(transformRequest: TransformRequest): Promise<ProcessResult<string>> {
		return this.postAsync<TransformRequest, ProcessResult<string>>('transform', transformRequest);
	}

	/**
	 * Transforms input with table transformer using SPARD rules.
	 * @param transformRequest Transformation request.
	 * @returns Transformation result including comparison with standard transformer.
	 */
	async transformTableAsync(transformRequest: TransformRequest): Promise<TransformTableResult> {
		return this.postAsync<TransformRequest, TransformTableResult>('transform/table', transformRequest);
	}

	/**
	 * Creates table transformation visualization.
	 * @param transform SPARD transformation rules.
	 * @returns Table with transformation rules.
	 */
	async generateTableAsync(transform: string): Promise<ProcessResult<string>> {
		return this.postAsync<string, ProcessResult<string>>('spard/table', transform);
	}

	/**
	 * Generates source code for SPARD rules.
	 * @param transform SPARD transformation rules.
	 * @returns C# source code for transformation rules.
	 */
	async generateSourceCodeAsync(transform: string): Promise<ProcessResult<string>> {
		return this.postAsync<string, ProcessResult<string>>('spard/source', transform);
	}

	private async getAsync<T>(uri: string) {
		const response = await fetch(
			`${this.options.serviceUri}/api/v1/${uri}`, this.options.culture ? {
				headers: {
					'Accept-Language': this.options.culture
				}
			} : undefined
		);

		if (!response.ok) {
			throw new Error(`Error while retrieving ${uri}: ${response.status} ${await response.text()}`);
		}

		return <T>(await response.json());
	}

	private async postAsync<TRequest, TResponse>(uri: string, request: TRequest) {
		const response = await fetch(
			`${this.options.serviceUri}/api/v1/${uri}`, {
				method: 'POST',
				headers: this.options.culture ? {
					...JSON_HEADERS,
					'Accept-Language': this.options.culture
				} : JSON_HEADERS,
				body: JSON.stringify(request)
			}
		);

		if (!response.ok) {
			throw new Error(`Error while retrieving ${uri}: ${response.status} ${await response.text()}`);
		}

		return <TResponse>(await response.json());
	}
}