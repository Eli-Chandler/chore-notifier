import { defineConfig } from 'orval';

export default defineConfig({
    'chore-notifier': {

        input: '../backend/ChoreNotifier/openapi/ChoreNotifier.json',
        output: {
            mode: 'tags-split',
            target: 'src/api',
            client: 'react-query',
            override: {
                query: {
                    useQuery: true,
                    useInfinite: true,
                    useInfiniteQueryParam: 'afterId',
                    useMutation: true,
                    useInvalidate: true
                }
            }
        }
    },
});
