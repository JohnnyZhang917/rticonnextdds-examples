using System;
using System.Collections.Generic;
using System.Text;
/* msg_publisher.cs

   A publication of data of type msg

   This file is derived from code automatically generated by the rtiddsgen 
   command:

   rtiddsgen -language C# -example <arch> msg.idl

   Example publication of type msg automatically generated by 
   'rtiddsgen'. To test them follow these steps:

   (1) Compile this file and the example subscription.

   (2) Start the subscription with the command
       objs\<arch>\msg_subscriber <domain_id> <sample_count>
                
   (3) Start the publication with the command
       objs\<arch>\msg_publisher <domain_id> <sample_count>

   (4) [Optional] Specify the list of discovery initial peers and 
       multicast receive addresses via an environment variable or a file 
       (in the current working directory) called NDDS_DISCOVERY_PEERS. 

   You can run any number of publishers and subscribers programs, and can 
   add and remove them dynamically from the domain.


   Example:

       To run the example application on domain <domain_id>:

       bin\<Debug|Release>\msg_publisher <domain_id> <sample_count>
       bin\<Debug|Release>\msg_subscriber <domain_id> <sample_count>

       
modification history
------------ -------       
*/

public class msgPublisher {
    // Authorization string.
    private const String auth = "password";

    /* Set up a linked list of authorized participant keys.  Datareaders associated
     * with an authorized participant do not need to supply their own password.
     */
    private static LinkedList<DDS.BuiltinTopicKey_t> auth_list = new LinkedList<DDS.BuiltinTopicKey_t>();

    public static void add_auth_participant(DDS.BuiltinTopicKey_t participant_key) {
        auth_list.AddLast(participant_key);
    }

    public static Boolean is_auth_participant(DDS.BuiltinTopicKey_t participant_key) {
        return auth_list.Contains(participant_key);
    }

    /* The builtin subscriber sets participant_qos.user_data and
	 * reader_qos.user_data, so we set up listeners for the builtin
	 * DataReaders to access these fields.
	 */

    public class BuiltinParticipantListener : DDS.DataReaderListener {
        DDS.ParticipantBuiltinTopicDataSeq data_seq =
            new DDS.ParticipantBuiltinTopicDataSeq();
        DDS.SampleInfoSeq info_seq = new DDS.SampleInfoSeq();

        // This gets called when a participant has been discovered
        public override void on_data_available(DDS.DataReader reader) {
            DDS.ParticipantBuiltinTopicDataDataReader builtin_reader =
                (DDS.ParticipantBuiltinTopicDataDataReader) reader;
            String participant_data;
            DDS.ParticipantBuiltinTopicData cur_participant_builtin_topic_data;

            try {
                // We only process newly seen participants
                builtin_reader.take(
                    data_seq, info_seq,
                    DDS.ResourceLimitsQosPolicy.LENGTH_UNLIMITED,
                    DDS.SampleStateKind.ANY_SAMPLE_STATE,
                    DDS.ViewStateKind.NEW_VIEW_STATE,
                    DDS.InstanceStateKind.ANY_INSTANCE_STATE);

                for(int i = 0; i < data_seq.length; ++i) {
                    DDS.SampleInfo info = (DDS.SampleInfo) info_seq.get_at(i);

                    if (info.valid_data == true) {
                        participant_data = "nil";
                        Boolean is_auth = false;
                        cur_participant_builtin_topic_data =
                            (DDS.ParticipantBuiltinTopicData) data_seq.get_at(i);

                        // see if there is any participant_data
                        if (cur_participant_builtin_topic_data.user_data.value.length > 0) {

                            //This sequences is guaranteed to be contiguous
                            participant_data = System.Text.Encoding.Default.GetString(
                                cur_participant_builtin_topic_data.user_data.value.buffer);
                            if (participant_data.Equals(auth)) {
                                add_auth_participant(cur_participant_builtin_topic_data.key);
                                is_auth = true;
                            }

                            //if (cur_participant_builtin_topic_data.user_data.value ==
                            //    System.Text.Encoding.Default.) {
                            //    add_auth_participant(cur_participant_builtin_topic_data.key);
                            //    is_auth = true;
                            //}
                        }
                        Console.WriteLine("Built-in Reader: found participant \n\tkey->'" + 
                            cur_participant_builtin_topic_data.key.GetHashCode() + 
                            "'\n\tuser_data->'" + participant_data + "'");
                        Console.WriteLine("instance_handle: " + info.instance_handle);
                        if (is_auth == false) {
                            Console.WriteLine("Bad authorization, ignoring participant");
                            DDS.DomainParticipant participant = reader.get_subscriber().get_participant();
                            DDS.InstanceHandle_t temp = info.instance_handle;
                            participant.ignore_participant(ref temp);
                            info.instance_handle = temp;
                        }
                    }
                }
            } catch (DDS.Retcode_NoData) {
                // No data to process
          	    // This happens when we get announcements from participants we
                // already know about
                return;
            } finally {
                builtin_reader.return_loan(data_seq, info_seq);
            }
        }
    }

    public class BuiltinSubscriberListener: DDS.DataReaderListener {
        DDS.SubscriptionBuiltinTopicDataSeq data_seq = new DDS.SubscriptionBuiltinTopicDataSeq();
        DDS.SampleInfoSeq info_seq = new DDS.SampleInfoSeq();

        // This gets called when a new subscriber has been discovered
        public override void on_data_available( DDS.DataReader reader ) {
            DDS.SubscriptionBuiltinTopicDataDataReader builtin_reader =
                (DDS.SubscriptionBuiltinTopicDataDataReader)reader;
            String reader_data;
            DDS.SubscriptionBuiltinTopicData cur_subscription_builtin_topic_data;

            try {
                //We only process newly seen subscribers
                builtin_reader.take(
                    data_seq, info_seq,
                    DDS.ResourceLimitsQosPolicy.LENGTH_UNLIMITED,
                    DDS.SampleStateKind.ANY_SAMPLE_STATE,
                    DDS.ViewStateKind.NEW_VIEW_STATE,
                    DDS.InstanceStateKind.ANY_INSTANCE_STATE);

                for (int i = 0; i < data_seq.length; ++i) {
                    DDS.SampleInfo info = (DDS.SampleInfo) info_seq.get_at(i);

                    if (info.valid_data == true) {
                        reader_data = "nil";
                        Boolean is_auth = false;
                        cur_subscription_builtin_topic_data = 
                            (DDS.SubscriptionBuiltinTopicData) data_seq.get_at(i);

                        // See if this is associated with an authorized participant
                        if (is_auth_participant(cur_subscription_builtin_topic_data.participant_key)) {
                            is_auth = true;
                        }

                        // See if there is any user_data
                        if (cur_subscription_builtin_topic_data.user_data.value.length > 0) {
                            reader_data = System.Text.Encoding.Default.GetString(
                                    cur_subscription_builtin_topic_data.user_data.value.buffer);
                            if (is_auth == false && reader_data.Equals(auth)) {
                                is_auth = true;
                            }
                        }

                        Console.WriteLine("Built-in Reader: found subscriber \n\tparticipant_key->'" +
                            cur_subscription_builtin_topic_data.participant_key.GetHashCode() +
                            "'\n\tkey-> '" + cur_subscription_builtin_topic_data.key.GetHashCode() + 
                            "'\n\tuser_data-> '" + reader_data + "'");
                        Console.WriteLine("instance_handle: " + info.instance_handle);
                        if(is_auth == false) {
                            Console.WriteLine("Bad autorhization, ignoring subscription");
                            DDS.DomainParticipant participant = reader.get_subscriber().get_participant();
                            DDS.InstanceHandle_t temp = info.instance_handle;
                            participant.ignore_subscription(ref temp);
                            info.instance_handle = temp;
                        }
                    }
                }
            } catch (DDS.Retcode_NoData) {
                // No data to process
                return;
            } finally {
                builtin_reader.return_loan(data_seq, info_seq);
            }
        }
    }

    /* End changes for builtin_topic */

    public static void Main(string[] args) {

        // --- Get domain ID --- //
        int domain_id = 0;
        if (args.Length >= 1) {
            domain_id = Int32.Parse(args[0]);
        }

        // --- Get max loop count; 0 means infinite loop  --- //
        int sample_count = 0;
        if (args.Length >= 2) {
            sample_count = Int32.Parse(args[1]);
        }

        /* Uncomment this to turn on additional logging
        NDDS.ConfigLogger.get_instance().set_verbosity_by_category(
            NDDS.LogCategory.NDDS_CONFIG_LOG_CATEGORY_API, 
            NDDS.LogVerbosity.NDDS_CONFIG_LOG_VERBOSITY_STATUS_ALL);
        */
    
        // --- Run --- //
        try {
            msgPublisher.publish(
                domain_id, sample_count);
        } catch(DDS.Exception) {
            Console.WriteLine("error in publisher");
        }
    }

    static void publish(int domain_id, int sample_count) {

        // --- Create participant --- //

        /* To customize participant QoS, use 
           the configuration file USER_QOS_PROFILES.xml */
        DDS.DomainParticipant participant =
            DDS.DomainParticipantFactory.get_instance().create_participant(
                domain_id,
                DDS.DomainParticipantFactory.PARTICIPANT_QOS_DEFAULT,
                null /* listener */,
                DDS.StatusMask.STATUS_MASK_NONE);
        if (participant == null) {
            shutdown(participant);
            throw new ApplicationException("create_participant error");
        }

        
        /* Start changes for Builtin_Topics */

        /* If you want to change the Participant's QoS programmatically rather than
         * using the XML file, you will need to add the following lines to your
         * code and comment out the participant call above.
         */

        /* By default, the participant is enabled upon construction.
         * At that time our listeners for the builtin topics have not
         * been installed, so we disable the participant until we
         * set up the listeners.
         */
/*
        DDS.DomainParticipantFactoryQos factory_qos = new DDS.DomainParticipantFactoryQos();
        DDS.DomainParticipantFactory.get_instance().get_qos(factory_qos);
        factory_qos.entity_factory.autoenable_created_entities = false;
        DDS.DomainParticipantFactory.get_instance().set_qos(factory_qos);

        // Get default participant QoS to customize
        DDS.DomainParticipantQos participant_qos = new DDS.DomainParticipantQos();
        DDS.DomainParticipantFactory.get_instance().get_default_participant_qos(participant_qos);

        participant_qos.discovery_config.participant_liveliness_assert_period.sec = 10;
        participant_qos.discovery_config.participant_liveliness_assert_period.nanosec = 0;

        participant_qos.discovery_config.participant_liveliness_lease_duration.sec = 12;
        participant_qos.discovery_config.participant_liveliness_lease_duration.nanosec = 0;

        DDS.DomainParticipant participant =
            DDS.DomainParticipantFactory.get_instance().create_participant(
                domain_id,
                participant_qos,
                null,
                DDS.StatusMask.STATUS_MASK_NONE);
        if (participant == null) {
            shutdown(participant);
            throw new ApplicationException("create_participant error");
        }
*/

        // Installing listeners for the builting topics requires several steps

        // First get the builting subscriber
        DDS.Subscriber builtin_subscriber = participant.get_builtin_subscriber();
        if (builtin_subscriber == null) {
            shutdown(participant);
            throw new ApplicationException("create_builtin_subscriber error");
        }

        /* Then get builtin subscriber's datareader for participants
	     * The type name is a bit hairy, but can be read right to left:
	     * DDS.ParticipantBuiltinTopicDataDataReader is a 
	     * DataReader for BuiltinTopicData concerning a discovered
	     * Participant
	     */
        DDS.ParticipantBuiltinTopicDataDataReader builtin_participant_datareader =
            (DDS.ParticipantBuiltinTopicDataDataReader)builtin_subscriber.lookup_datareader(
                DDS.ParticipantBuiltinTopicDataTypeSupport.PARTICIPANT_TOPIC_NAME);

        // Install our listener
        BuiltinParticipantListener builtin_participant_listener =
            new BuiltinParticipantListener();
        try {
            builtin_participant_datareader.set_listener(
                builtin_participant_listener,
                    (DDS.StatusMask.STATUS_MASK_NONE |
                    (DDS.StatusMask)DDS.StatusKind.DATA_AVAILABLE_STATUS));
        } catch (DDS.Exception e) {
            shutdown(participant);
            Console.WriteLine("set_listener error: {0}", e);
        }
        // Get builtin subscriber's datareader for subscribers
        DDS.SubscriptionBuiltinTopicDataDataReader builtin_subscription_datareader =
            (DDS.SubscriptionBuiltinTopicDataDataReader)
                builtin_subscriber.lookup_datareader(
                DDS.SubscriptionBuiltinTopicDataTypeSupport.SUBSCRIPTION_TOPIC_NAME);
        if (builtin_participant_datareader == null) {
            shutdown(participant);
            throw new ApplicationException("lookup_datareader error");
        }

        // Install our listener
        BuiltinSubscriberListener builtin_subscriber_listener = 
            new BuiltinSubscriberListener();
        builtin_subscription_datareader.set_listener(
            builtin_subscriber_listener,
                (DDS.StatusMask.STATUS_MASK_NONE |
                (DDS.StatusMask)DDS.StatusKind.DATA_AVAILABLE_STATUS));

        // Done!  All the listeners are installed, so we can enable the participant now.
        participant.enable();

        /* End changes for Builtin_Topics */

        // --- Create publisher --- //

        /* To customize publisher QoS, use 
           the configuration file USER_QOS_PROFILES.xml */
        DDS.Publisher publisher = participant.create_publisher(
            DDS.DomainParticipant.PUBLISHER_QOS_DEFAULT,
            null /* listener */,
            DDS.StatusMask.STATUS_MASK_NONE);
        if (publisher == null) {
            shutdown(participant);
            throw new ApplicationException("create_publisher error");
        }

        // --- Create topic --- //

        /* Register type before creating topic */
        System.String type_name = msgTypeSupport.get_type_name();
        try {
            msgTypeSupport.register_type(
                participant, type_name);
        }
        catch(DDS.Exception e) {
            Console.WriteLine("register_type error {0}", e);
            shutdown(participant);
            throw e;
        }

        /* To customize topic QoS, use 
           the configuration file USER_QOS_PROFILES.xml */
        DDS.Topic topic = participant.create_topic(
            "Example msg",
            type_name,
            DDS.DomainParticipant.TOPIC_QOS_DEFAULT,
            null /* listener */,
            DDS.StatusMask.STATUS_MASK_NONE);
        if (topic == null) {
            shutdown(participant);
            throw new ApplicationException("create_topic error");
        }

        // --- Create writer --- //

        msgListener writer_listener = new msgListener();

        /* To customize data writer QoS, use 
           the configuration file USER_QOS_PROFILES.xml */
        DDS.DataWriter writer = publisher.create_datawriter(
            topic,
            DDS.Publisher.DATAWRITER_QOS_DEFAULT,
            writer_listener,
            DDS.StatusMask.STATUS_MASK_NONE);
        if (writer == null) {
            shutdown(participant);
            throw new ApplicationException("create_datawriter error");
        }

        msgDataWriter msg_writer = (msgDataWriter)writer;

        // --- Write --- //

        /* Create data sample for writing */
        msg instance = msgTypeSupport.create_data();
        if (instance == null) {
            shutdown(participant);
            throw new ApplicationException(
                "msgTypeSupport.create_data error");
        }

        /* For a data type that has a key, if the same instance is going to be
           written multiple times, initialize the key here
           and register the keyed instance prior to writing */
        DDS.InstanceHandle_t instance_handle = DDS.InstanceHandle_t.HANDLE_NIL;
        /*
        instance_handle = msg_writer.register_instance(instance);
        */

        /* Main loop */
        const System.Int32 send_period = 1000; // milliseconds

        /* Changes for Builtin_Topics */
        for (short count=0;
             (sample_count == 0) || (count < sample_count);
             ++count) {
            
            System.Threading.Thread.Sleep(send_period);
            Console.WriteLine("Writing msg, count {0}", count);

            /* Modify the data to be sent here */
            /* Changes for Builtin_Topics */
            instance.x = count;

            try {
                msg_writer.write(instance, ref instance_handle);
            }
            catch(DDS.Exception e) {
                Console.WriteLine("write error {0}", e);
            }
        }

        /*
        try {
            msg_writer.unregister_instance(
                instance, ref instance_handle);
        } catch(DDS.Exception e) {
            Console.WriteLine("unregister instance error: {0}", e);
        }
        */

        // --- Shutdown --- //

        /* Delete data sample */
        try {
            msgTypeSupport.delete_data(instance);
        } catch(DDS.Exception e) {
            Console.WriteLine(
                "msgTypeSupport.delete_data error: {0}", e);
        }

        /* Delete all entities */
        shutdown(participant);
    }

    static void shutdown(
        DDS.DomainParticipant participant) {

        /* Delete all entities */

        if (participant != null) {
            participant.delete_contained_entities();
            DDS.DomainParticipantFactory.get_instance().delete_participant(
                ref participant);
        }

        /* RTI Connext provides finalize_instance() method on
           domain participant factory for people who want to release memory
           used by the participant factory. Uncomment the following block of
           code for clean destruction of the singleton. */
        /*
        try {
            DDS.DomainParticipantFactory.finalize_instance();
        } catch (DDS.Exception e) {
            Console.WriteLine("finalize_instance error: {0}", e);
            throw e;
        }
        */
    }

    private class msgListener : DDS.DataWriterListener {
    	public void on_liveliness_lost(DDS.DataWriter writer, DDS.LivelinessLostStatus status) {
    		Console.WriteLine("liveliness lost, total count = " + status.total_count);    		
    	}
    	public void on_publication_matched (DDS.DataWriter writer, DDS.PublicationMatchedStatus status) {
    		Console.WriteLine("publication_matched, current count = " + status.current_count);
    	}
    }
}

