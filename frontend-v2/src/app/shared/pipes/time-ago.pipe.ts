import { Pipe, PipeTransform } from '@angular/core';
import { formatDistanceToNow, differenceInSeconds  } from 'date-fns';

@Pipe({
    name: 'timeAgo',
    standalone: true,
    pure: false,
})
export class TimeAgoPipe implements PipeTransform {
    transform(value: string | Date | null | undefined): string {
        if (!value) return '';
        const date = typeof value === 'string' ? new Date(value) : value;
        const seconds = differenceInSeconds(new Date(), date);
        if (seconds < 30) return 'just now';
        return formatDistanceToNow(date, { addSuffix: true });
    }
}