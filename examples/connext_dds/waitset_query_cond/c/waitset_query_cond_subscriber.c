/*******************************************************************************
 (c) 2005-2014 Copyright, Real-Time Innovations, Inc.  All rights reserved.
 RTI grants Licensee a license to use, modify, compile, and create derivative
 works of the Software.  Licensee has the right to distribute object form only
 for use with RTI products.  The Software is provided "as is", with no warranty
 of any type, including any warranty for fitness for any purpose. RTI is under
 no obligation to maintain or support the Software.  RTI shall not be liable for
 any incidental or consequential damages arising out of the use or inability to
 use the software.
 ******************************************************************************/
/* waitset_query_cond_subscriber.c

   A subscription example

   This file is derived from code automatically generated by the rtiddsgen
   command:

   rtiddsgen -language C -example <arch> waitset_query_cond.idl

   Example subscription of type waitset_query_cond automatically generated by
   'rtiddsgen'. To test them, follow these steps:

   (1) Compile this file and the example publication.

   (2) Start the subscription with the command
       objs/<arch>/waitset_query_cond_subscriber <domain_id> <sample_count>

   (3) Start the publication with the command
       objs/<arch>/waitset_query_cond_publisher <domain_id> <sample_count>

   (4) [Optional] Specify the list of discovery initial peers and
       multicast receive addresses via an environment variable or a file
       (in the current working directory) called NDDS_DISCOVERY_PEERS.

   You can run any number of publishers and subscribers programs, and can
   add and remove them dynamically from the domain.


   Example:

       To run the example application on domain <domain_id>:

       On UNIX systems:

       objs/<arch>/waitset_query_cond_publisher <domain_id>
       objs/<arch>/waitset_query_cond_subscriber <domain_id>

       On Windows systems:

       objs\<arch>\waitset_query_cond_publisher <domain_id>
       objs\<arch>\waitset_query_cond_subscriber <domain_id>


modification history
------------ -------
*/

#include "ndds/ndds_c.h"
#include "waitset_query_cond.h"
#include "waitset_query_condSupport.h"
#include <stdio.h>
#include <stdlib.h>

/* We don't need to use listeners as we are going to use Waitsets and Conditions
 * so we have removed the auto generated code for listeners here
 */

/* Delete all entities */
static int subscriber_shutdown(DDS_DomainParticipant *participant)
{
    DDS_ReturnCode_t retcode;
    int status = 0;

    if (participant != NULL) {
        retcode = DDS_DomainParticipant_delete_contained_entities(participant);
        if (retcode != DDS_RETCODE_OK) {
            printf("delete_contained_entities error %d\n", retcode);
            status = -1;
        }

        retcode = DDS_DomainParticipantFactory_delete_participant(
                DDS_TheParticipantFactory,
                participant);
        if (retcode != DDS_RETCODE_OK) {
            printf("delete_participant error %d\n", retcode);
            status = -1;
        }
    }

    /* RTI Connext provides the finalize_instance() method on
       domain participant factory for users who want to release memory used
       by the participant factory. Uncomment the following block of code for
       clean destruction of the singleton. */
    /*
        retcode = DDS_DomainParticipantFactory_finalize_instance();
        if (retcode != DDS_RETCODE_OK) {
            printf("finalize_instance error %d\n", retcode);
            status = -1;
        }
    */

    return status;
}

static int subscriber_main(int domainId, int sample_count)
{
    DDS_DomainParticipant *participant = NULL;
    DDS_Subscriber *subscriber = NULL;
    DDS_Topic *topic = NULL;
    DDS_DataReader *reader = NULL;
    DDS_ReturnCode_t retcode;
    const char *type_name = NULL;
    int count = 0;
    struct DDS_Duration_t wait_timeout = { 1, 0 };
    /* Additional variables for this example */
    int i;
    DDS_WaitSet *waitset = NULL;
    DDS_QueryCondition *query_condition = NULL;
    waitset_query_condDataReader *waitset_query_cond_reader = NULL;
    const char *query_expression = DDS_String_dup("name MATCH %0");
    struct DDS_StringSeq query_parameters;
    struct DDS_ConditionSeq active_conditions_seq = DDS_SEQUENCE_INITIALIZER;
    struct waitset_query_condSeq data_seq = DDS_SEQUENCE_INITIALIZER;
    struct DDS_SampleInfoSeq info_seq = DDS_SEQUENCE_INITIALIZER;
    /* Auxiliary variables */
    char *odd_string = DDS_String_dup("'ODD'");
    char *even_string = DDS_String_dup("'EVEN'");
    /* The initial value of the param_list is EVEN string */
    const char *param_list[] = { even_string };

    /* To customize participant QoS, use
       the configuration file USER_QOS_PROFILES.xml */
    participant = DDS_DomainParticipantFactory_create_participant(
            DDS_TheParticipantFactory,
            domainId,
            &DDS_PARTICIPANT_QOS_DEFAULT,
            NULL /* listener */,
            DDS_STATUS_MASK_NONE);
    if (participant == NULL) {
        printf("create_participant error\n");
        subscriber_shutdown(participant);
        return -1;
    }

    /* To customize subscriber QoS, use
       the configuration file USER_QOS_PROFILES.xml */
    subscriber = DDS_DomainParticipant_create_subscriber(
            participant,
            &DDS_SUBSCRIBER_QOS_DEFAULT,
            NULL /* listener */,
            DDS_STATUS_MASK_NONE);
    if (subscriber == NULL) {
        printf("create_subscriber error\n");
        subscriber_shutdown(participant);
        return -1;
    }

    /* Register the type before creating the topic */
    type_name = waitset_query_condTypeSupport_get_type_name();
    retcode =
            waitset_query_condTypeSupport_register_type(participant, type_name);
    if (retcode != DDS_RETCODE_OK) {
        printf("register_type error %d\n", retcode);
        subscriber_shutdown(participant);
        return -1;
    }

    /* To customize topic QoS, use
       the configuration file USER_QOS_PROFILES.xml */
    topic = DDS_DomainParticipant_create_topic(
            participant,
            "Example waitset_query_cond",
            type_name,
            &DDS_TOPIC_QOS_DEFAULT,
            NULL /* listener */,
            DDS_STATUS_MASK_NONE);
    if (topic == NULL) {
        printf("create_topic error\n");
        subscriber_shutdown(participant);
        return -1;
    }

    /* To customize data reader QoS, use
       the configuration file USER_QOS_PROFILES.xml */
    reader = DDS_Subscriber_create_datareader(
            subscriber,
            DDS_Topic_as_topicdescription(topic),
            &DDS_DATAREADER_QOS_DEFAULT,
            NULL,
            DDS_STATUS_MASK_NONE);
    if (reader == NULL) {
        printf("create_datareader error\n");
        subscriber_shutdown(participant);
        return -1;
    }

    /* Narrow the reader into your specific data type */
    waitset_query_cond_reader = waitset_query_condDataReader_narrow(reader);
    if (waitset_query_cond_reader == NULL) {
        printf("DataReader narrow error\n");
        return -1;
    }

    /* Create query condition */

    DDS_StringSeq_initialize(&query_parameters);
    DDS_StringSeq_set_maximum(&query_parameters, 1);

    /*Here we set the default filter using the param_list */
    DDS_StringSeq_from_array(&query_parameters, param_list, 1);

    query_condition = DDS_DataReader_create_querycondition(
            reader,
            DDS_NOT_READ_SAMPLE_STATE,
            DDS_ANY_VIEW_STATE,
            DDS_ANY_INSTANCE_STATE,
            query_expression,
            &query_parameters);
    if (query_condition == NULL) {
        printf("create_query_condition error\n");
        subscriber_shutdown(participant);
        return -1;
    }

    waitset = DDS_WaitSet_new();
    if (waitset == NULL) {
        printf("create waitset error\n");
        subscriber_shutdown(participant);
        return -1;
    }

    /* Attach Query Conditions */
    retcode = DDS_WaitSet_attach_condition(
            waitset,
            (DDS_Condition *) query_condition);
    if (retcode != DDS_RETCODE_OK) {
        printf("attach_condition error\n");
        subscriber_shutdown(participant);
        return -1;
    }

    printf("\n>>>Timeout: %.0d sec & %d nanosec\n",
           wait_timeout.sec,
           wait_timeout.nanosec);
    printf(">>> Query conditions: name MATCH %%0\n");
    printf("\t%%0 = %s\n", param_list[0]);
    printf("---------------------------------\n\n");

    /* Main loop */
    for (count = 0; (sample_count == 0) || (count < sample_count); ++count) {
        /* We set a new parameter in the Query Condition after 7 secs */
        if (count == 7) {
            param_list[0] = odd_string;
            printf("CHANGING THE QUERY CONDITION\n");
            printf("\n>>> Query conditions: name MATCH %%0\n");
            printf("\t%%0 = %s\n", param_list[0]);
            printf(">>> We keep one sample in the history\n");
            printf("-------------------------------------\n\n");
            DDS_StringSeq_from_array(&query_parameters, param_list, 1);
            DDS_QueryCondition_set_query_parameters(
                    query_condition,
                    &query_parameters);
        }
        /* wait() blocks execution of the thread until one or more attached
         * Conditions become true, or until a user-specified timeout expires.
         */
        retcode = DDS_WaitSet_wait(
                waitset,                /* waitset */
                &active_conditions_seq, /* active conditions sequence */
                &wait_timeout);         /* timeout */

        /* We get to timeout if no conditions were triggered */
        if (retcode == DDS_RETCODE_TIMEOUT) {
            printf("Wait timed out!! No conditions were triggered.\n");
            continue;
        } else if (retcode != DDS_RETCODE_OK) {
            printf("wait returned error: %d", retcode);
            break;
        }

        retcode = DDS_RETCODE_OK;
        while (retcode != DDS_RETCODE_NO_DATA) {
            retcode = waitset_query_condDataReader_take_w_condition(
                    waitset_query_cond_reader,
                    &data_seq,
                    &info_seq,
                    DDS_LENGTH_UNLIMITED,
                    DDS_QueryCondition_as_readcondition(query_condition));

            for (i = 0; i < waitset_query_condSeq_get_length(&data_seq); ++i) {
                if (!DDS_SampleInfoSeq_get_reference(&info_seq, i)
                             ->valid_data) {
                    printf("Got metadata\n");
                    continue;
                }
                waitset_query_condTypeSupport_print_data(
                        waitset_query_condSeq_get_reference(&data_seq, i));
            }
            waitset_query_condDataReader_return_loan(
                    waitset_query_cond_reader,
                    &data_seq,
                    &info_seq);
        }
    }

    /* Cleanup and delete all entities */
    return subscriber_shutdown(participant);
}

#if defined(RTI_WINCE)
int wmain(int argc, wchar_t **argv)
{
    int domainId = 0;
    int sample_count = 0; /* infinite loop */

    if (argc >= 2) {
        domainId = _wtoi(argv[1]);
    }
    if (argc >= 3) {
        sample_count = _wtoi(argv[2]);
    }

    /* Uncomment this to turn on additional logging
    NDDS_Config_Logger_set_verbosity_by_category(
        NDDS_Config_Logger_get_instance(),
        NDDS_CONFIG_LOG_CATEGORY_API,
        NDDS_CONFIG_LOG_VERBOSITY_STATUS_ALL);
    */

    return subscriber_main(domainId, sample_count);
}
#elif !(defined(RTI_VXWORKS) && !defined(__RTP__)) && !defined(RTI_PSOS)
int main(int argc, char *argv[])
{
    int domainId = 0;
    int sample_count = 0; /* infinite loop */

    if (argc >= 2) {
        domainId = atoi(argv[1]);
    }
    if (argc >= 3) {
        sample_count = atoi(argv[2]);
    }

    /* Uncomment this to turn on additional logging
    NDDS_Config_Logger_set_verbosity_by_category(
        NDDS_Config_Logger_get_instance(),
        NDDS_CONFIG_LOG_CATEGORY_API,
        NDDS_CONFIG_LOG_VERBOSITY_STATUS_ALL);
    */

    return subscriber_main(domainId, sample_count);
}
#endif

#ifdef RTI_VX653
const unsigned char *__ctype = NULL;

void usrAppInit()
{
    #ifdef USER_APPL_INIT
    USER_APPL_INIT; /* for backwards compatibility */
    #endif

    /* add application specific code here */
    taskSpawn(
            "sub",
            RTI_OSAPI_THREAD_PRIORITY_NORMAL,
            0x8,
            0x150000,
            (FUNCPTR) subscriber_main,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
}
#endif
