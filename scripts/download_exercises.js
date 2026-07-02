const fs = require('fs');
const path = require('path');

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

async function downloadExercises() {
    let exercises = [];
    let after = '';
    let hasNext = true;
    let page = 1;

    console.log('Starting download from ExerciseDB with rate-limit handling...');

    while (hasNext) {
        let url = 'https://oss.exercisedb.dev/api/v1/exercises?limit=25';
        if (after) {
            url += `&after=${after}`;
        }

        console.log(`Fetching page ${page}...`);
        try {
            const response = await fetch(url);
            if (response.status === 429) {
                console.warn('Rate limited (429). Waiting 8 seconds...');
                await sleep(8000);
                continue;
            }
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const result = await response.json();
            if (result.success && result.data) {
                exercises = exercises.concat(result.data);
                after = result.meta.nextCursor;
                hasNext = result.meta.hasNextPage;
                console.log(`Fetched ${result.data.length} exercises (Total: ${exercises.length})`);
                page++;
                
                // Be polite to the API to avoid 429s
                await sleep(1000);
            } else {
                console.error('Failed to parse response structure:', result);
                hasNext = false;
            }
        } catch (error) {
            console.error('Fetch error:', error);
            console.log('Retrying in 4 seconds...');
            await sleep(4000);
        }
    }

    console.log(`Downloaded ${exercises.length} raw exercises.`);

    // Map to our schema
    const formatted = exercises.map(ex => {
        return {
            exerciseId: ex.exerciseId,
            name: ex.name,
            gifUrl: ex.gifUrl,
            bodyPart: ex.bodyParts && ex.bodyParts.length > 0 ? ex.bodyParts[0] : '',
            muscle: ex.targetMuscles && ex.targetMuscles.length > 0 ? ex.targetMuscles[0] : '',
            equipment: ex.equipments && ex.equipments.length > 0 ? ex.equipments[0] : null,
            instructions: ex.instructions ? ex.instructions.join('\n') : ''
        };
    });

    const outputPath = path.join(__dirname, '..', 'Resources', 'Raw', 'exercises.json');
    fs.mkdirSync(path.dirname(outputPath), { recursive: true });
    fs.writeFileSync(outputPath, JSON.stringify(formatted, null, 2), 'utf8');
    console.log(`Successfully wrote ${formatted.length} exercises to ${outputPath}`);
}

downloadExercises();
