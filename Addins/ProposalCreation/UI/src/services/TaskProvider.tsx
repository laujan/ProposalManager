import { ITaskProvider } from './ITaskProvider';
import { ApiService } from './ApiService';

export class TaskProvider implements ITaskProvider {
    
    private tasks: string[];
    
    constructor(apiService: ApiService) {
        let that = this;
        apiService.callApi('Task', '', 'GET', []).then(data => that.tasks = data as string[]);
    }

    getTasks(): string[] {
        return this.tasks;
    }

}